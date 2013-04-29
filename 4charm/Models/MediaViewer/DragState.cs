/* 
    Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
    Use of this sample source code is subject to the terms of the Microsoft license 
    agreement under which you licensed this sample source code and is provided AS-IS.
    If you did not accept the terms of the license agreement, you are not authorized 
    to use this sample source code.  For the terms of the license, please see the 
    license agreement between you and Microsoft.
  
    To see all Code Samples for Windows Phone, visit http://go.microsoft.com/fwlink/?LinkID=219604 
  
*/
using System;

namespace Microsoft.Phone.Controls
{
    internal class DragState
    {
        public double MaxDraggingBoundary { get; set; }
        public double MinDraggingBoundary { get; set; }
        public bool GotDragDelta { get; set; }
        public bool IsDraggingFirstElement { get; set; }
        public bool IsDraggingLastElement { get; set; }
        public DateTime LastDragUpdateTime { get; set; }
        public double DragStartingMediaStripOffset { get; set; }
        public double NetDragDistanceSincleLastDragStagnation { get; set; }
        public double LastDragDistanceDelta { get; set; }
        public int NewDisplayedElementIndex { get; set; }
        public double UnsquishTranslationAnimationTarget { get; set; }

        public DragState(double maxDraggingBoundary)
        {
            this.MaxDraggingBoundary = maxDraggingBoundary;
        }
    }
}
