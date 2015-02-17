/*
 *  Copyright (c) 2014 The WebM project authors. All Rights Reserved.
 *
 *  Use of this source code is governed by a BSD-style license
 *  that can be found in the LICENSE file in the root of the source
 *  tree. An additional intellectual property rights grant can be found
 *  in the file PATENTS.  All contributing project authors may
 *  be found in the AUTHORS file in the root of the source tree.
 */

#include <limits.h>
#include <math.h>

#include "vp9/encoder/vp9_aq_cyclicrefresh.h"

#include "vp9/common/vp9_seg_common.h"

#include "vp9/encoder/vp9_ratectrl.h"
#include "vp9/encoder/vp9_segmentation.h"

struct CYCLIC_REFRESH {
  // Percentage of blocks per frame that are targeted as candidates
  // for cyclic refresh.
  int percent_refresh;
  // Maximum q-delta as percentage of base q.
  int max_qdelta_perc;
  // Superblock starting index for cycling through the frame.
  int sb_index;
  // Controls how long block will need to wait to be refreshed again, in
  // excess of the cycle time, i.e., in the case of all zero motion, block
  // will be refreshed every (100/percent_refresh + time_for_refresh) frames.
  int time_for_refresh;
  // // Target number of (8x8) blocks that are set for delta-q (segment 1).
  int target_num_seg_blocks;
  // Actual number of (8x8) blocks that were applied delta-q (segment 1).
  int actual_num_seg_blocks;
  // RD mult. parameters for segment 1.
  int rdmult;
  // Cyclic refresh map.
  signed char *map;
  // Thresholds applied to the projected rate/distortion of the coding block,
  // when deciding whether block should be refreshed.
  int64_t thresh_rate_sb;
  int64_t thresh_dist_sb;
  // Threshold applied to the motion vector (in units of 1/8 pel) of the
  // coding block, when deciding whether block should be refreshed.
  int16_t motion_thresh;
  // Rate target ratio to set q delta.
  double rate_ratio_qdelta;
};

CYCLIC_REFRESH *vp9_cyclic_refresh_alloc(int mi_rows, int mi_cols) {
  CYCLIC_REFRESH *const cr = vpx_calloc(1, sizeof(*cr));
  if (cr == NULL)
    return NULL;

  cr->map = vpx_calloc(mi_rows * mi_cols, sizeof(*cr->map));
  if (cr->map == NULL) {
    vpx_free(cr);
    return NULL;
  }

  return cr;
}

void vp9_cyclic_refresh_free(CYCLIC_REFRESH *cr) {
  vpx_free(cr->map);
  vpx_free(cr);
}

// Check if we should turn off cyclic refresh based on bitrate condition.
static int apply_cyclic_refresh_bitrate(const VP9_COMMON *cm,
                                        const RATE_CONTROL *rc) {
  // Turn off cyclic refresh if bits available per frame is not sufficiently
  // larger than bit cost of segmentation. Segment map bit cost should scale
  // with number of seg blocks, so compare available bits to number of blocks.
  // Average bits available per frame = avg_frame_bandwidth
  // Number of (8x8) blocks in frame = mi_rows * mi_cols;
  const float factor  = 0.5;
  const int number_blocks = cm->mi_rows  * cm->mi_cols;
  // The condition below corresponds to turning off at target bitrates:
  // ~24kbps for CIF, 72kbps for VGA (at 30fps).
  // Also turn off at very small frame sizes, to avoid too large fraction of
  // superblocks to be refreshed per frame. Threshold below is less than QCIF.
  if (rc->avg_frame_bandwidth < factor * number_blocks ||
      number_blocks / 64 < 5)
    return 0;
  else
    return 1;
}

// Check if this coding block, of size bsize, should be considered for refresh
// (lower-qp coding). Decision can be based on various factors, such as
// size of the coding block (i.e., below min_block size rejected), coding
// mode, and rate/distortion.
static int candidate_refresh_aq(const CYCLIC_REFRESH *cr,
                                const MB_MODE_INFO *mbmi,
                                int64_t rate,
                                int64_t dist) {
  MV mv = mbmi->mv[0].as_mv;
  // If projected rate is below the thresh_rate accept it for lower-qp coding.
  // Otherwise, reject the block for lower-qp coding if projected distortion
  // is above the threshold, and any of the following is true:
  // 1) mode uses large mv
  // 2) mode is an intra-mode
  if (rate < cr->thresh_rate_sb)
    return 1;
  else if (dist > cr->thresh_dist_sb &&
          (mv.row > cr->motion_thresh || mv.row < -cr->motion_thresh ||
           mv.col > cr->motion_thresh || mv.col < -cr->motion_thresh ||
           !is_inter_block(mbmi)))
    return 0;
  else
    return 1;
}

// Compute delta-q for the segment.
static int compute_deltaq(const VP9_COMP *cpi, int q) {
  const CYCLIC_REFRESH *const cr = cpi->cyclic_refresh;
  const RATE_CONTROL *const rc = &cpi->rc;
  int deltaq = vp9_compute_qdelta_by_rate(rc, cpi->common.frame_type,
                                          q, cr->rate_ratio_qdelta,
                                          cpi->common.bit_depth);
  if ((-deltaq) > cr->max_qdelta_perc * q / 100) {
    deltaq = -cr->max_qdelta_perc * q / 100;
  }
  return deltaq;
}

// For the just encoded frame, estimate the bits, incorporating the delta-q
// from segment 1. This function is called in the postencode (called from
// rc_update_rate_correction_factors()).
int vp9_cyclic_refresh_estimate_bits_at_q(const VP9_COMP *cpi,
                                          double correction_factor) {
  const VP9_COMMON *const cm = &cpi->common;
  const CYCLIC_REFRESH *const cr = cpi->cyclic_refresh;
  int estimated_bits;
  int mbs = cm->MBs;
  int num8x8bl = mbs << 2;
  // Weight for segment 1: use actual number of blocks refreshed in
  // previous/just encoded frame. Note number of blocks here is in 8x8 units.
  double weight_segment = (double)cr->actual_num_seg_blocks / num8x8bl;
  // Compute delta-q that was used in the just encoded frame.
  int deltaq = compute_deltaq(cpi, cm->base_qindex);
  // Take segment weighted average for estimated bits.
  estimated_bits = (int)((1.0 - weight_segment) *
      vp9_estimate_bits_at_q(cm->frame_type, cm->base_qindex, mbs,
                             correction_factor, cm->bit_depth) +
                             weight_segment *
      vp9_estimate_bits_at_q(cm->frame_type, cm->base_qindex + deltaq, mbs,
                             correction_factor, cm->bit_depth));
  return estimated_bits;
}

// Prior to encoding the frame, estimate the bits per mb, for a given q = i and
// a corresponding delta-q (for segment 1). This function is called in the
// rc_regulate_q() to set the base qp index.
int vp9_cyclic_refresh_rc_bits_per_mb(const VP9_COMP *cpi, int i,
                                      double correction_factor) {
  const VP9_COMMON *const cm = &cpi->common;
  CYCLIC_REFRESH *const cr = cpi->cyclic_refresh;
  int bits_per_mb;
  int num8x8bl = cm->MBs << 2;
  // Weight for segment 1 prior to encoding: take the target number for the
  // frame to be encoded. Number of blocks here is in 8x8 units.
  // Note that this is called in rc_regulate_q, which is called before the
  // cyclic_refresh_setup (which sets cr->target_num_seg_blocks). So a mismatch
  // may occur between the cr->target_num_seg_blocks value here and the
  // cr->target_num_seg_block set for encoding the frame. For the current use
  // case of fixed cr->percent_refresh and cr->time_for_refresh = 0, mismatch
  // does not occur/is very small.
  double weight_segment = (double)cr->target_num_seg_blocks / num8x8bl;
  // Compute delta-q corresponding to qindex i.
  int deltaq = compute_deltaq(cpi, i);
  // Take segment weighted average for bits per mb.
  bits_per_mb = (int)((1.0 - weight_segment) *
      vp9_rc_bits_per_mb(cm->frame_type, i, correction_factor, cm->bit_depth) +
      weight_segment *
      vp9_rc_bits_per_mb(cm->frame_type, i + deltaq, correction_factor,
                         cm->bit_depth));
  return bits_per_mb;
}

// Prior to coding a given prediction block, of size bsize at (mi_row, mi_col),
// check if we should reset the segment_id, and update the cyclic_refresh map
// and segmentation map.
void vp9_cyclic_refresh_update_segment(VP9_COMP *const cpi,
                                       MB_MODE_INFO *const mbmi,
                                       int mi_row, int mi_col,
                                       BLOCK_SIZE bsize,
                                       int64_t rate,
                                       int64_t dist) {
  const VP9_COMMON *const cm = &cpi->common;
  CYCLIC_REFRESH *const cr = cpi->cyclic_refresh;
  const int bw = num_8x8_blocks_wide_lookup[bsize];
  const int bh = num_8x8_blocks_high_lookup[bsize];
  const int xmis = MIN(cm->mi_cols - mi_col, bw);
  const int ymis = MIN(cm->mi_rows - mi_row, bh);
  const int block_index = mi_row * cm->mi_cols + mi_col;
  const int refresh_this_block = candidate_refresh_aq(cr, mbmi, rate, dist);
  // Default is to not update the refresh map.
  int new_map_value = cr->map[block_index];
  int x = 0; int y = 0;

  // Check if we should reset the segment_id for this block.
  if (mbmi->segment_id > 0 && !refresh_this_block)
    mbmi->segment_id = 0;

  // Update the cyclic refresh map, to be used for setting segmentation map
  // for the next frame. If the block  will be refreshed this frame, mark it
  // as clean. The magnitude of the -ve influences how long before we consider
  // it for refresh again.
  if (mbmi->segment_id == 1) {
    new_map_value = -cr->time_for_refresh;
  } else if (refresh_this_block) {
    // Else if it is accepted as candidate for refresh, and has not already
    // been refreshed (marked as 1) then mark it as a candidate for cleanup
    // for future time (marked as 0), otherwise don't update it.
    if (cr->map[block_index] == 1)
      new_map_value = 0;
  } else {
    // Leave it marked as block that is not candidate for refresh.
    new_map_value = 1;
  }

  // Update entries in the cyclic refresh map with new_map_value, and
  // copy mbmi->segment_id into global segmentation map.
  for (y = 0; y < ymis; y++)
    for (x = 0; x < xmis; x++) {
      cr->map[block_index + y * cm->mi_cols + x] = new_map_value;
      cpi->segmentation_map[block_index + y * cm->mi_cols + x] =
          mbmi->segment_id;
    }
}

// Update the actual number of blocks that were applied the segment delta q.
void vp9_cyclic_refresh_update_actual_count(struct VP9_COMP *const cpi) {
  VP9_COMMON *const cm = &cpi->common;
  CYCLIC_REFRESH *const cr = cpi->cyclic_refresh;
  unsigned char *const seg_map = cpi->segmentation_map;
  int mi_row, mi_col;
  cr->actual_num_seg_blocks = 0;
  for (mi_row = 0; mi_row < cm->mi_rows; mi_row++)
  for (mi_col = 0; mi_col < cm->mi_cols; mi_col++) {
    if (seg_map[mi_row * cm->mi_cols + mi_col] == 1)
      cr->actual_num_seg_blocks++;
  }
}

// Update the segmentation map, and related quantities: cyclic refresh map,
// refresh sb_index, and target number of blocks to be refreshed.
void vp9_cyclic_refresh_update_map(VP9_COMP *const cpi) {
  VP9_COMMON *const cm = &cpi->common;
  CYCLIC_REFRESH *const cr = cpi->cyclic_refresh;
  unsigned char *const seg_map = cpi->segmentation_map;
  int i, block_count, bl_index, sb_rows, sb_cols, sbs_in_frame;
  int xmis, ymis, x, y;
  vpx_memset(seg_map, 0, cm->mi_rows * cm->mi_cols);
  sb_cols = (cm->mi_cols + MI_BLOCK_SIZE - 1) / MI_BLOCK_SIZE;
  sb_rows = (cm->mi_rows + MI_BLOCK_SIZE - 1) / MI_BLOCK_SIZE;
  sbs_in_frame = sb_cols * sb_rows;
  // Number of target blocks to get the q delta (segment 1).
  block_count = cr->percent_refresh * cm->mi_rows * cm->mi_cols / 100;
  // Set the segmentation map: cycle through the superblocks, starting at
  // cr->mb_index, and stopping when either block_count blocks have been found
  // to be refreshed, or we have passed through whole frame.
  assert(cr->sb_index < sbs_in_frame);
  i = cr->sb_index;
  cr->target_num_seg_blocks = 0;
  do {
    int sum_map = 0;
    // Get the mi_row/mi_col corresponding to superblock index i.
    int sb_row_index = (i / sb_cols);
    int sb_col_index = i - sb_row_index * sb_cols;
    int mi_row = sb_row_index * MI_BLOCK_SIZE;
    int mi_col = sb_col_index * MI_BLOCK_SIZE;
    assert(mi_row >= 0 && mi_row < cm->mi_rows);
    assert(mi_col >= 0 && mi_col < cm->mi_cols);
    bl_index = mi_row * cm->mi_cols + mi_col;
    // Loop through all 8x8 blocks in superblock and update map.
    xmis = MIN(cm->mi_cols - mi_col,
               num_8x8_blocks_wide_lookup[BLOCK_64X64]);
    ymis = MIN(cm->mi_rows - mi_row,
               num_8x8_blocks_high_lookup[BLOCK_64X64]);
    for (y = 0; y < ymis; y++) {
      for (x = 0; x < xmis; x++) {
        const int bl_index2 = bl_index + y * cm->mi_cols + x;
        // If the block is as a candidate for clean up then mark it
        // for possible boost/refresh (segment 1). The segment id may get
        // reset to 0 later if block gets coded anything other than ZEROMV.
        if (cr->map[bl_index2] == 0) {
          sum_map++;
        } else if (cr->map[bl_index2] < 0) {
          cr->map[bl_index2]++;
        }
      }
    }
    // Enforce constant segment over superblock.
    // If segment is at least half of superblock, set to 1.
    if (sum_map >= xmis * ymis / 2) {
      for (y = 0; y < ymis; y++)
        for (x = 0; x < xmis; x++) {
          seg_map[bl_index + y * cm->mi_cols + x] = 1;
        }
      cr->target_num_seg_blocks += xmis * ymis;
    }
    i++;
    if (i == sbs_in_frame) {
      i = 0;
    }
  } while (cr->target_num_seg_blocks < block_count && i != cr->sb_index);
  cr->sb_index = i;
}

// Set/update global/frame level cyclic refresh parameters.
void vp9_cyclic_refresh_update_parameters(VP9_COMP *const cpi) {
  const RATE_CONTROL *const rc = &cpi->rc;
  CYCLIC_REFRESH *const cr = cpi->cyclic_refresh;
  cr->percent_refresh = 10;
  // Use larger delta-qp (increase rate_ratio_qdelta) for first few (~4)
  // periods of the refresh cycle, after a key frame. This corresponds to ~40
  // frames with cr->percent_refresh = 10.
  if (rc->frames_since_key <  40)
    cr->rate_ratio_qdelta = 3.0;
  else
    cr->rate_ratio_qdelta = 2.0;
}

// Setup cyclic background refresh: set delta q and segmentation map.
void vp9_cyclic_refresh_setup(VP9_COMP *const cpi) {
  VP9_COMMON *const cm = &cpi->common;
  const RATE_CONTROL *const rc = &cpi->rc;
  CYCLIC_REFRESH *const cr = cpi->cyclic_refresh;
  struct segmentation *const seg = &cm->seg;
  const int apply_cyclic_refresh  = apply_cyclic_refresh_bitrate(cm, rc);
  // Don't apply refresh on key frame or enhancement layer frames.
  if (!apply_cyclic_refresh ||
      (cm->frame_type == KEY_FRAME) ||
      (cpi->svc.temporal_layer_id > 0) ||
      (cpi->svc.spatial_layer_id > 0)) {
    // Set segmentation map to 0 and disable.
    unsigned char *const seg_map = cpi->segmentation_map;
    vpx_memset(seg_map, 0, cm->mi_rows * cm->mi_cols);
    vp9_disable_segmentation(&cm->seg);
    if (cm->frame_type == KEY_FRAME)
      cr->sb_index = 0;
    return;
  } else {
    int qindex_delta = 0;
    int qindex2;
    const double q = vp9_convert_qindex_to_q(cm->base_qindex, cm->bit_depth);
    vp9_clear_system_state();
    cr->max_qdelta_perc = 50;
    cr->time_for_refresh = 0;
    // Set rate threshold to some fraction (set to 1 for now) of the target
    // rate (target is given by sb64_target_rate and scaled by 256).
    cr->thresh_rate_sb = (rc->sb64_target_rate << 8);
    // Distortion threshold, quadratic in Q, scale factor to be adjusted.
    cr->thresh_dist_sb = (int)(q * q) << 5;
    cr->motion_thresh = 32;
    // Set up segmentation.
    // Clear down the segment map.
    vp9_enable_segmentation(&cm->seg);
    vp9_clearall_segfeatures(seg);
    // Select delta coding method.
    seg->abs_delta = SEGMENT_DELTADATA;

    // Note: setting temporal_update has no effect, as the seg-map coding method
    // (temporal or spatial) is determined in vp9_choose_segmap_coding_method(),
    // based on the coding cost of each method. For error_resilient mode on the
    // last_frame_seg_map is set to 0, so if temporal coding is used, it is
    // relative to 0 previous map.
    // seg->temporal_update = 0;

    // Segment 0 "Q" feature is disabled so it defaults to the baseline Q.
    vp9_disable_segfeature(seg, 0, SEG_LVL_ALT_Q);
    // Use segment 1 for in-frame Q adjustment.
    vp9_enable_segfeature(seg, 1, SEG_LVL_ALT_Q);

    // Set the q delta for segment 1.
    qindex_delta = compute_deltaq(cpi, cm->base_qindex);

    // Compute rd-mult for segment 1.
    qindex2 = clamp(cm->base_qindex + cm->y_dc_delta_q + qindex_delta, 0, MAXQ);
    cr->rdmult = vp9_compute_rd_mult(cpi, qindex2);

    vp9_set_segdata(seg, 1, SEG_LVL_ALT_Q, qindex_delta);

    // Update the segmentation and refresh map.
    vp9_cyclic_refresh_update_map(cpi);
  }
}

int vp9_cyclic_refresh_get_rdmult(const CYCLIC_REFRESH *cr) {
  return cr->rdmult;
}
