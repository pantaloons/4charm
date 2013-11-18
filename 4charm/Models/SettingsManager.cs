using _4charm.Models.Migration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace _4charm.Models
{
    abstract class SettingsManager
    {
        public void WaitForPartialWrites()
        {
            if (_partialWriteTask != null)
            {
                _partialWriteTask.Wait();
            }

            if (_queuedSaveTask != null)
            {
                if (_queuedSaveTask.Status == TaskStatus.Created)
                {
                    _queuedSaveTask.Start();
                }

                _queuedSaveTask.Unwrap().Wait();
            }
        }

        protected T GetSetting<T>(string name, T defaultValue)
        {
            Restore().Wait();

            if (_settings.ContainsKey(name) && _settings[name] is T)
            {
                return (T)_settings[name];
            }
            else
            {
                return (T)defaultValue;
            }
        }

        protected void SetSetting<T>(string name, T value)
        {
            Restore().Wait();

            _settings[name] = value;

            Save();
        }

        private string _fileName;
        private List<Type> _knownTypes;
        private Dictionary<string, object> _settings = new Dictionary<string, object>();

        private Task _restoreTask = null;
        private Task _partialWriteTask = null;
        private Task<Task> _saveTask = null;
        private Task<Task> _queuedSaveTask = null;

        protected SettingsManager(string fileName, List<Type> knownTypes)
        {
            _fileName = fileName;
            _knownTypes = knownTypes;
            Restore();
        }

        /// <summary>
        /// Save the current settings object.
        /// </summary>
        /// <returns>An asynchronous task that reflects when settings have been saved.</returns>
        private Task Save()
        {
            // Dragons! We don't want multiple saves to happen at once, since they can't concurrently write
            // to the save file, but SaveAsync is asynchronous so a single thread (the UI thread) can itself
            // try to concurrently write.

            // Instead, when we get a save request-- we queue it up to run as a continuation of all previous
            // save requests. The saves will be executed "asynchronously" (without blocking), but serially,
            // on the UI thread.

            // If there is at least one unscheduled continuation queued, we don't need to add another
            // since that save operation will pick up the changes that generated this save too, we can
            // safely drop the save request.
            if (_saveTask == null)
            {
                // We have to call SaveAsync() immediately to ensure the partialWriteTask gets generated,
                // otherwise the first save call could be dropped by ignored wait for partial writes.
                Task t = SaveAsync();
                _saveTask = new Task<Task>(() => t);
                _saveTask.Start(TaskScheduler.FromCurrentSynchronizationContext());
            }
            else if (_queuedSaveTask == null || (_queuedSaveTask.Status != TaskStatus.WaitingForActivation &&
                                                 _queuedSaveTask.Status != TaskStatus.WaitingForChildrenToComplete &&
                                                 _queuedSaveTask.Status != TaskStatus.WaitingToRun &&
                                                 _queuedSaveTask.Status != TaskStatus.Created))
            {
                // Either there isn't a continuation scheduled and we need to create one, or there is a continuation scheduled
                // but it's already started executing and we can't be sure it picked up the state changes, so schedule a new
                // one anyway.
                Task<Task> continuation = new Task<Task>(SaveAsync);
                _queuedSaveTask = continuation;
                _saveTask = _saveTask.Unwrap().ContinueWith(t =>
                {
                    // If someone flushed pending writes, the continuation could have already been scheduled (and completed)
                    if (continuation.Status == TaskStatus.Created)
                    {
                        continuation.Start(TaskScheduler.FromCurrentSynchronizationContext());
                    }
                    return continuation.Unwrap();
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

            return _saveTask;
        }

        private async Task SaveAsync()
        {
            try
            {
                // Serialize the settings synchronously to avoid asynchronous access to shared state
                MemoryStream sessionData = new MemoryStream();
                DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>), _knownTypes);
                serializer.WriteObject(sessionData, _settings);

                // The write task should just happen asynchronously on the UI thread, but SL app deactivation/closing events
                // are received on the UI so there would be no way to ensure these save tasks complete. In Jupiter we can
                // request a deferral, but for now they are run off UI thread so we can block it on deactivation.
                _partialWriteTask = Task.Run(async () =>
                {
                    // Get an output stream for the settings file and write the state asynchronously
                    StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(_fileName, CreationCollisionOption.ReplaceExisting);
                    using (Stream fileStream = await file.OpenStreamForWriteAsync())
                    {
                        sessionData.Seek(0, SeekOrigin.Begin);
                        await sessionData.CopyToAsync(fileStream);
                        await fileStream.FlushAsync();
                    }
                });
                await _partialWriteTask;
            }
            catch
            {
                // Do nothing
            }
        }

        /// <summary>
        /// Restores previously saved settings.
        /// </summary>
        /// <returns>An asynchronous task that reflects when settings have been read. The
        /// content of _settings should not be relied upon until this task completes.</returns>
        protected Task Restore()
        {
            if (_restoreTask == null)
            {
                _restoreTask = Task.Run(async () => await RestoreAsync());
            }

            return _restoreTask;
        }

        private async Task RestoreAsync()
        {
            _settings = new Dictionary<String, Object>();

            try
            {
                // Get the input stream for the SessionState file
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(_fileName);
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    // Deserialize the Session State
                    DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>), _knownTypes);
                    _settings = (Dictionary<string, object>)serializer.ReadObject(inStream.AsStreamForRead());
                }
            }
            catch (FileNotFoundException)
            {
                // If there is no modern settings file, this is probably their first application run
                // and we may need to migrate their old settings.
                if (_fileName == CriticalSettingsManager.Current._fileName)
                {
                    // Version migration must happen synchronously, to avoid concurrent
                    // access to the settings object while migrating. This perf hit is
                    // acceptable, since it is one time only.
                    VersionMigrator.Migrate1_1to1_2(_settings);

                    // We just wrote directly into the settings object, so we need to manually
                    // queue a save operation. That can happen later though, it does not need
                    // to be synchronous.
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(async () =>
                    {
                        await Save();
                    });
                }
            }
            catch
            {
                // Do nothing, just start again with empty settings object.
            }
        }
    }
}
