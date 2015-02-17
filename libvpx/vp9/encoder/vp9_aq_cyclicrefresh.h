/*
 *  Copyright (c) 2014 The WebM project authors. All Rights Reserved.
 *
 *  Use of this source code is governed by a BSD-style license
 *  that can be found in the LICENSE file in the root of the source
 *  tree. An additional intellectual property rights grant can be found
 *  in the file PATENTS.  All contributing project authors may
 *  be found in the AUTHORS file in the root of the source tree.
 */


#ifndef VP9_ENCODER_VP9_AQ_CYCLICREFRESH_H_
#define VP9_ENCODER_VP9_AQ_CYCLICREFRESH_H_

#include "vp9/common/vp9_blockd.h"

#ifdef __cplusplus
extern "C" {
#endif

struct VP9_COMP;

struct CYCLIC_REFRESH;
typedef struct CYCLIC_REFRESH CYCLIC_REFRESH;

CYCLIC_REFRESH *vp9_cyclic_refresh_alloc(int mi_rows, int mi_cols);

void vp9_cyclic_refresh_free(CYCLIC_REFRESH *cr);

// Estimate the bits, incorporating the delta-q from segment 1, after encoding
// the frame.
int vp9_cyclic_refresh_estimate_bits_at_q(const struct VP9_COMP *cpi,
                                          double correction_factor);

// Estimate the bits per mb, for a given q = i and a corresponding delta-q
// (for segment 1), prior to encoding the frame.
int vp9_cyclic_refresh_rc_bits_per_mb(const struct VP9_COMP *cpi, int i,
                                      double correction_factor);

// Prior to coding a given prediction block, of size bsize at (mi_row, mi_col),
// check if we should reset the segment_id, and update the cyclic_refresh map
// and segmentation map.
void vp9_cyclic_refresh_update_segment(struct VP9_COMP *const cpi,
                                       MB_MODE_INFO *const mbmi,
                                       int mi_row, int mi_col, BLOCK_SIZE bsize,
                                       int64_t rate, int64_t dist);

// Update the segmentation map, and related quantities: cyclic refresh map,
// refresh sb_index, and target number of blocks to be refreshed.
void vp9_cyclic_refresh_update__map(struct VP9_COMP *const cpi);

// Update the actual number of blocks that were applied the segment delta q.
void vp9_cyclic_refresh_update_actual_count(struct VP9_COMP *const cpi);

// Set/update global/frame level refresh parameters.
void vp9_cyclic_refresh_update_parameters(struct VP9_COMP *const cpi);

// Setup cyclic background refresh: set delta q and segmentation map.
void vp9_cyclic_refresh_setup(struct VP9_COMP *const cpi);

int vp9_cyclic_refresh_get_rdmult(const CYCLIC_REFRESH *cr);

#ifdef __cplusplus
}  // extern "C"
#endif

#endif  // VP9_ENCODER_VP9_AQ_CYCLICREFRESH_H_
