/*
 *  Copyright 2011 The LibYuv Project Authors. All rights reserved.
 *
 *  Use of this source code is governed by a BSD-style license
 *  that can be found in the LICENSE file in the root of the source
 *  tree. An additional intellectual property rights grant can be found
 *  in the file PATENTS. All contributing project authors may
 *  be found in the AUTHORS file in the root of the source tree.
 */

#include "libyuv/row.h"

#ifdef __cplusplus
namespace libyuv {
extern "C" {
#endif

// This module is for GCC Neon
#if !defined(LIBYUV_DISABLE_NEON) && defined(__aarch64__)

// Read 8 Y, 4 U and 4 V from 422
#define READYUV422                                                             \
    MEMACCESS(0)                                                               \
    "vld1.8     {d0}, [%0]!                    \n"                             \
    MEMACCESS(1)                                                               \
    "vld1.32    {d2[0]}, [%1]!                 \n"                             \
    MEMACCESS(2)                                                               \
    "vld1.32    {d2[1]}, [%2]!                 \n"

// Read 8 Y, 2 U and 2 V from 422
#define READYUV411                                                             \
    MEMACCESS(0)                                                               \
    "vld1.8     {d0}, [%0]!                    \n"                             \
    MEMACCESS(1)                                                               \
    "vld1.16    {d2[0]}, [%1]!                 \n"                             \
    MEMACCESS(2)                                                               \
    "vld1.16    {d2[1]}, [%2]!                 \n"                             \
    "vmov.u8    d3, d2                         \n"                             \
    "vzip.u8    d2, d3                         \n"

// Read 8 Y, 8 U and 8 V from 444
#define READYUV444                                                             \
    MEMACCESS(0)                                                               \
    "vld1.8     {d0}, [%0]!                    \n"                             \
    MEMACCESS(1)                                                               \
    "vld1.8     {d2}, [%1]!                    \n"                             \
    MEMACCESS(2)                                                               \
    "vld1.8     {d3}, [%2]!                    \n"                             \
    "vpaddl.u8  q1, q1                         \n"                             \
    "vrshrn.u16 d2, q1, #1                     \n"

// Read 8 Y, and set 4 U and 4 V to 128
#define READYUV400                                                             \
    MEMACCESS(0)                                                               \
    "vld1.8     {d0}, [%0]!                    \n"                             \
    "vmov.u8    d2, #128                       \n"

// Read 8 Y and 4 UV from NV12
#define READNV12                                                               \
    MEMACCESS(0)                                                               \
    "vld1.8     {d0}, [%0]!                    \n"                             \
    MEMACCESS(1)                                                               \
    "vld1.8     {d2}, [%1]!                    \n"                             \
    "vmov.u8    d3, d2                         \n"/* split odd/even uv apart */\
    "vuzp.u8    d2, d3                         \n"                             \
    "vtrn.u32   d2, d3                         \n"

// Read 8 Y and 4 VU from NV21
#define READNV21                                                               \
    MEMACCESS(0)                                                               \
    "vld1.8     {d0}, [%0]!                    \n"                             \
    MEMACCESS(1)                                                               \
    "vld1.8     {d2}, [%1]!                    \n"                             \
    "vmov.u8    d3, d2                         \n"/* split odd/even uv apart */\
    "vuzp.u8    d3, d2                         \n"                             \
    "vtrn.u32   d2, d3                         \n"

// Read 8 YUY2
#define READYUY2                                                               \
    MEMACCESS(0)                                                               \
    "vld2.8     {d0, d2}, [%0]!                \n"                             \
    "vmov.u8    d3, d2                         \n"                             \
    "vuzp.u8    d2, d3                         \n"                             \
    "vtrn.u32   d2, d3                         \n"

// Read 8 UYVY
#define READUYVY                                                               \
    MEMACCESS(0)                                                               \
    "vld2.8     {d2, d3}, [%0]!                \n"                             \
    "vmov.u8    d0, d3                         \n"                             \
    "vmov.u8    d3, d2                         \n"                             \
    "vuzp.u8    d2, d3                         \n"                             \
    "vtrn.u32   d2, d3                         \n"

#define YUV422TORGB                                                            \
    "veor.u8    d2, d26                        \n"/*subtract 128 from u and v*/\
    "vmull.s8   q8, d2, d24                    \n"/*  u/v B/R component      */\
    "vmull.s8   q9, d2, d25                    \n"/*  u/v G component        */\
    "vmov.u8    d1, #0                         \n"/*  split odd/even y apart */\
    "vtrn.u8    d0, d1                         \n"                             \
    "vsub.s16   q0, q0, q15                    \n"/*  offset y               */\
    "vmul.s16   q0, q0, q14                    \n"                             \
    "vadd.s16   d18, d19                       \n"                             \
    "vqadd.s16  d20, d0, d16                   \n" /* B */                     \
    "vqadd.s16  d21, d1, d16                   \n"                             \
    "vqadd.s16  d22, d0, d17                   \n" /* R */                     \
    "vqadd.s16  d23, d1, d17                   \n"                             \
    "vqadd.s16  d16, d0, d18                   \n" /* G */                     \
    "vqadd.s16  d17, d1, d18                   \n"                             \
    "vqshrun.s16 d0, q10, #6                   \n" /* B */                     \
    "vqshrun.s16 d1, q11, #6                   \n" /* G */                     \
    "vqshrun.s16 d2, q8, #6                    \n" /* R */                     \
    "vmovl.u8   q10, d0                        \n"/*  set up for reinterleave*/\
    "vmovl.u8   q11, d1                        \n"                             \
    "vmovl.u8   q8, d2                         \n"                             \
    "vtrn.u8    d20, d21                       \n"                             \
    "vtrn.u8    d22, d23                       \n"                             \
    "vtrn.u8    d16, d17                       \n"                             \
    "vmov.u8    d21, d16                       \n"

static vec8 kUVToRB  = { 127, 127, 127, 127, 102, 102, 102, 102,
                         0, 0, 0, 0, 0, 0, 0, 0 };
static vec8 kUVToG = { -25, -25, -25, -25, -52, -52, -52, -52,
                       0, 0, 0, 0, 0, 0, 0, 0 };

#ifdef HAS_I444TOARGBROW_NEON
void I444ToARGBRow_NEON(const uint8* src_y,
                        const uint8* src_u,
                        const uint8* src_v,
                        uint8* dst_argb,
                        int width) {
  asm volatile (
    MEMACCESS(5)
    "vld1.8     {d24}, [%5]                    \n"
    MEMACCESS(6)
    "vld1.8     {d25}, [%6]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUV444
    YUV422TORGB
    "subs       %4, %4, #8                     \n"
    "vmov.u8    d23, #255                      \n"
    MEMACCESS(3)
    "vst4.8     {d20, d21, d22, d23}, [%3]!    \n"
    "bgt        1b                             \n"
    : "+r"(src_y),     // %0
      "+r"(src_u),     // %1
      "+r"(src_v),     // %2
      "+r"(dst_argb),  // %3
      "+r"(width)      // %4
    : "r"(&kUVToRB),   // %5
      "r"(&kUVToG)     // %6
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_I444TOARGBROW_NEON

#ifdef HAS_I422TOARGBROW_NEON
void I422ToARGBRow_NEON(const uint8* src_y,
                        const uint8* src_u,
                        const uint8* src_v,
                        uint8* dst_argb,
                        int width) {
  asm volatile (
    MEMACCESS(5)
    "vld1.8     {d24}, [%5]                    \n"
    MEMACCESS(6)
    "vld1.8     {d25}, [%6]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUV422
    YUV422TORGB
    "subs       %4, %4, #8                     \n"
    "vmov.u8    d23, #255                      \n"
    MEMACCESS(3)
    "vst4.8     {d20, d21, d22, d23}, [%3]!    \n"
    "bgt        1b                             \n"
    : "+r"(src_y),     // %0
      "+r"(src_u),     // %1
      "+r"(src_v),     // %2
      "+r"(dst_argb),  // %3
      "+r"(width)      // %4
    : "r"(&kUVToRB),   // %5
      "r"(&kUVToG)     // %6
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_I422TOARGBROW_NEON

#ifdef HAS_I411TOARGBROW_NEON
void I411ToARGBRow_NEON(const uint8* src_y,
                        const uint8* src_u,
                        const uint8* src_v,
                        uint8* dst_argb,
                        int width) {
  asm volatile (
    MEMACCESS(5)
    "vld1.8     {d24}, [%5]                    \n"
    MEMACCESS(6)
    "vld1.8     {d25}, [%6]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUV411
    YUV422TORGB
    "subs       %4, %4, #8                     \n"
    "vmov.u8    d23, #255                      \n"
    MEMACCESS(3)
    "vst4.8     {d20, d21, d22, d23}, [%3]!    \n"
    "bgt        1b                             \n"
    : "+r"(src_y),     // %0
      "+r"(src_u),     // %1
      "+r"(src_v),     // %2
      "+r"(dst_argb),  // %3
      "+r"(width)      // %4
    : "r"(&kUVToRB),   // %5
      "r"(&kUVToG)     // %6
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_I411TOARGBROW_NEON

#ifdef HAS_I422TOBGRAROW_NEON
void I422ToBGRARow_NEON(const uint8* src_y,
                        const uint8* src_u,
                        const uint8* src_v,
                        uint8* dst_bgra,
                        int width) {
  asm volatile (
    MEMACCESS(5)
    "vld1.8     {d24}, [%5]                    \n"
    MEMACCESS(6)
    "vld1.8     {d25}, [%6]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUV422
    YUV422TORGB
    "subs       %4, %4, #8                     \n"
    "vswp.u8    d20, d22                       \n"
    "vmov.u8    d19, #255                      \n"
    MEMACCESS(3)
    "vst4.8     {d19, d20, d21, d22}, [%3]!    \n"
    "bgt        1b                             \n"
    : "+r"(src_y),     // %0
      "+r"(src_u),     // %1
      "+r"(src_v),     // %2
      "+r"(dst_bgra),  // %3
      "+r"(width)      // %4
    : "r"(&kUVToRB),   // %5
      "r"(&kUVToG)     // %6
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_I422TOBGRAROW_NEON

#ifdef HAS_I422TOABGRROW_NEON
void I422ToABGRRow_NEON(const uint8* src_y,
                        const uint8* src_u,
                        const uint8* src_v,
                        uint8* dst_abgr,
                        int width) {
  asm volatile (
    MEMACCESS(5)
    "vld1.8     {d24}, [%5]                    \n"
    MEMACCESS(6)
    "vld1.8     {d25}, [%6]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUV422
    YUV422TORGB
    "subs       %4, %4, #8                     \n"
    "vswp.u8    d20, d22                       \n"
    "vmov.u8    d23, #255                      \n"
    MEMACCESS(3)
    "vst4.8     {d20, d21, d22, d23}, [%3]!    \n"
    "bgt        1b                             \n"
    : "+r"(src_y),     // %0
      "+r"(src_u),     // %1
      "+r"(src_v),     // %2
      "+r"(dst_abgr),  // %3
      "+r"(width)      // %4
    : "r"(&kUVToRB),   // %5
      "r"(&kUVToG)     // %6
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_I422TOABGRROW_NEON

#ifdef HAS_I422TORGBAROW_NEON
void I422ToRGBARow_NEON(const uint8* src_y,
                        const uint8* src_u,
                        const uint8* src_v,
                        uint8* dst_rgba,
                        int width) {
  asm volatile (
    MEMACCESS(5)
    "vld1.8     {d24}, [%5]                    \n"
    MEMACCESS(6)
    "vld1.8     {d25}, [%6]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUV422
    YUV422TORGB
    "subs       %4, %4, #8                     \n"
    "vmov.u8    d19, #255                      \n"
    MEMACCESS(3)
    "vst4.8     {d19, d20, d21, d22}, [%3]!    \n"
    "bgt        1b                             \n"
    : "+r"(src_y),     // %0
      "+r"(src_u),     // %1
      "+r"(src_v),     // %2
      "+r"(dst_rgba),  // %3
      "+r"(width)      // %4
    : "r"(&kUVToRB),   // %5
      "r"(&kUVToG)     // %6
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_I422TORGBAROW_NEON

#ifdef HAS_I422TORGB24ROW_NEON
void I422ToRGB24Row_NEON(const uint8* src_y,
                         const uint8* src_u,
                         const uint8* src_v,
                         uint8* dst_rgb24,
                         int width) {
  asm volatile (
    MEMACCESS(5)
    "vld1.8     {d24}, [%5]                    \n"
    MEMACCESS(6)
    "vld1.8     {d25}, [%6]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUV422
    YUV422TORGB
    "subs       %4, %4, #8                     \n"
    MEMACCESS(3)
    "vst3.8     {d20, d21, d22}, [%3]!         \n"
    "bgt        1b                             \n"
    : "+r"(src_y),      // %0
      "+r"(src_u),      // %1
      "+r"(src_v),      // %2
      "+r"(dst_rgb24),  // %3
      "+r"(width)       // %4
    : "r"(&kUVToRB),    // %5
      "r"(&kUVToG)      // %6
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_I422TORGB24ROW_NEON

#ifdef HAS_I422TORAWROW_NEON
void I422ToRAWRow_NEON(const uint8* src_y,
                       const uint8* src_u,
                       const uint8* src_v,
                       uint8* dst_raw,
                       int width) {
  asm volatile (
    MEMACCESS(5)
    "vld1.8     {d24}, [%5]                    \n"
    MEMACCESS(6)
    "vld1.8     {d25}, [%6]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUV422
    YUV422TORGB
    "subs       %4, %4, #8                     \n"
    "vswp.u8    d20, d22                       \n"
    MEMACCESS(3)
    "vst3.8     {d20, d21, d22}, [%3]!         \n"
    "bgt        1b                             \n"
    : "+r"(src_y),    // %0
      "+r"(src_u),    // %1
      "+r"(src_v),    // %2
      "+r"(dst_raw),  // %3
      "+r"(width)     // %4
    : "r"(&kUVToRB),  // %5
      "r"(&kUVToG)    // %6
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_I422TORAWROW_NEON

#define ARGBTORGB565                                                           \
    "vshr.u8    d20, d20, #3                   \n"  /* B                    */ \
    "vshr.u8    d21, d21, #2                   \n"  /* G                    */ \
    "vshr.u8    d22, d22, #3                   \n"  /* R                    */ \
    "vmovl.u8   q8, d20                        \n"  /* B                    */ \
    "vmovl.u8   q9, d21                        \n"  /* G                    */ \
    "vmovl.u8   q10, d22                       \n"  /* R                    */ \
    "vshl.u16   q9, q9, #5                     \n"  /* G                    */ \
    "vshl.u16   q10, q10, #11                  \n"  /* R                    */ \
    "vorr       q0, q8, q9                     \n"  /* BG                   */ \
    "vorr       q0, q0, q10                    \n"  /* BGR                  */

#ifdef HAS_I422TORGB565ROW_NEON
void I422ToRGB565Row_NEON(const uint8* src_y,
                          const uint8* src_u,
                          const uint8* src_v,
                          uint8* dst_rgb565,
                          int width) {
  asm volatile (
    MEMACCESS(5)
    "vld1.8     {d24}, [%5]                    \n"
    MEMACCESS(6)
    "vld1.8     {d25}, [%6]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUV422
    YUV422TORGB
    "subs       %4, %4, #8                     \n"
    ARGBTORGB565
    MEMACCESS(3)
    "vst1.8     {q0}, [%3]!                    \n"  // store 8 pixels RGB565.
    "bgt        1b                             \n"
    : "+r"(src_y),    // %0
      "+r"(src_u),    // %1
      "+r"(src_v),    // %2
      "+r"(dst_rgb565),  // %3
      "+r"(width)     // %4
    : "r"(&kUVToRB),  // %5
      "r"(&kUVToG)    // %6
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_I422TORGB565ROW_NEON

#define ARGBTOARGB1555                                                         \
    "vshr.u8    q10, q10, #3                   \n"  /* B                    */ \
    "vshr.u8    d22, d22, #3                   \n"  /* R                    */ \
    "vshr.u8    d23, d23, #7                   \n"  /* A                    */ \
    "vmovl.u8   q8, d20                        \n"  /* B                    */ \
    "vmovl.u8   q9, d21                        \n"  /* G                    */ \
    "vmovl.u8   q10, d22                       \n"  /* R                    */ \
    "vmovl.u8   q11, d23                       \n"  /* A                    */ \
    "vshl.u16   q9, q9, #5                     \n"  /* G                    */ \
    "vshl.u16   q10, q10, #10                  \n"  /* R                    */ \
    "vshl.u16   q11, q11, #15                  \n"  /* A                    */ \
    "vorr       q0, q8, q9                     \n"  /* BG                   */ \
    "vorr       q1, q10, q11                   \n"  /* RA                   */ \
    "vorr       q0, q0, q1                     \n"  /* BGRA                 */

#ifdef HAS_I422TOARGB1555ROW_NEON
void I422ToARGB1555Row_NEON(const uint8* src_y,
                            const uint8* src_u,
                            const uint8* src_v,
                            uint8* dst_argb1555,
                            int width) {
  asm volatile (
    MEMACCESS(5)
    "vld1.8     {d24}, [%5]                    \n"
    MEMACCESS(6)
    "vld1.8     {d25}, [%6]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUV422
    YUV422TORGB
    "subs       %4, %4, #8                     \n"
    "vmov.u8    d23, #255                      \n"
    ARGBTOARGB1555
    MEMACCESS(3)
    "vst1.8     {q0}, [%3]!                    \n"  // store 8 pixels ARGB1555.
    "bgt        1b                             \n"
    : "+r"(src_y),    // %0
      "+r"(src_u),    // %1
      "+r"(src_v),    // %2
      "+r"(dst_argb1555),  // %3
      "+r"(width)     // %4
    : "r"(&kUVToRB),  // %5
      "r"(&kUVToG)    // %6
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_I422TOARGB1555ROW_NEON

#define ARGBTOARGB4444                                                         \
    "vshr.u8    d20, d20, #4                   \n"  /* B                    */ \
    "vbic.32    d21, d21, d4                   \n"  /* G                    */ \
    "vshr.u8    d22, d22, #4                   \n"  /* R                    */ \
    "vbic.32    d23, d23, d4                   \n"  /* A                    */ \
    "vorr       d0, d20, d21                   \n"  /* BG                   */ \
    "vorr       d1, d22, d23                   \n"  /* RA                   */ \
    "vzip.u8    d0, d1                         \n"  /* BGRA                 */

#ifdef HAS_I422TOARGB4444ROW_NEON
void I422ToARGB4444Row_NEON(const uint8* src_y,
                            const uint8* src_u,
                            const uint8* src_v,
                            uint8* dst_argb4444,
                            int width) {
  asm volatile (
    MEMACCESS(5)
    "vld1.8     {d24}, [%5]                    \n"
    MEMACCESS(6)
    "vld1.8     {d25}, [%6]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    "vmov.u8    d4, #0x0f                      \n"  // bits to clear with vbic.
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUV422
    YUV422TORGB
    "subs       %4, %4, #8                     \n"
    "vmov.u8    d23, #255                      \n"
    ARGBTOARGB4444
    MEMACCESS(3)
    "vst1.8     {q0}, [%3]!                    \n"  // store 8 pixels ARGB4444.
    "bgt        1b                             \n"
    : "+r"(src_y),    // %0
      "+r"(src_u),    // %1
      "+r"(src_v),    // %2
      "+r"(dst_argb4444),  // %3
      "+r"(width)     // %4
    : "r"(&kUVToRB),  // %5
      "r"(&kUVToG)    // %6
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_I422TOARGB4444ROW_NEON

#ifdef HAS_YTOARGBROW_NEON
void YToARGBRow_NEON(const uint8* src_y,
                     uint8* dst_argb,
                     int width) {
  asm volatile (
    MEMACCESS(3)
    "vld1.8     {d24}, [%3]                    \n"
    MEMACCESS(4)
    "vld1.8     {d25}, [%4]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUV400
    YUV422TORGB
    "subs       %2, %2, #8                     \n"
    "vmov.u8    d23, #255                      \n"
    MEMACCESS(1)
    "vst4.8     {d20, d21, d22, d23}, [%1]!    \n"
    "bgt        1b                             \n"
    : "+r"(src_y),     // %0
      "+r"(dst_argb),  // %1
      "+r"(width)      // %2
    : "r"(&kUVToRB),   // %3
      "r"(&kUVToG)     // %4
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_YTOARGBROW_NEON

#ifdef HAS_I400TOARGBROW_NEON
void I400ToARGBRow_NEON(const uint8* src_y,
                        uint8* dst_argb,
                        int width) {
  asm volatile (
    ".p2align   2                              \n"
    "vmov.u8    d23, #255                      \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld1.8     {d20}, [%0]!                   \n"
    "vmov       d21, d20                       \n"
    "vmov       d22, d20                       \n"
    "subs       %2, %2, #8                     \n"
    MEMACCESS(1)
    "vst4.8     {d20, d21, d22, d23}, [%1]!    \n"
    "bgt        1b                             \n"
    : "+r"(src_y),     // %0
      "+r"(dst_argb),  // %1
      "+r"(width)      // %2
    :
    : "cc", "memory", "d20", "d21", "d22", "d23"
  );
}
#endif  // HAS_I400TOARGBROW_NEON

#ifdef HAS_NV12TOARGBROW_NEON
void NV12ToARGBRow_NEON(const uint8* src_y,
                        const uint8* src_uv,
                        uint8* dst_argb,
                        int width) {
  asm volatile (
    MEMACCESS(4)
    "vld1.8     {d24}, [%4]                    \n"
    MEMACCESS(5)
    "vld1.8     {d25}, [%5]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READNV12
    YUV422TORGB
    "subs       %3, %3, #8                     \n"
    "vmov.u8    d23, #255                      \n"
    MEMACCESS(2)
    "vst4.8     {d20, d21, d22, d23}, [%2]!    \n"
    "bgt        1b                             \n"
    : "+r"(src_y),     // %0
      "+r"(src_uv),    // %1
      "+r"(dst_argb),  // %2
      "+r"(width)      // %3
    : "r"(&kUVToRB),   // %4
      "r"(&kUVToG)     // %5
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_NV12TOARGBROW_NEON

#ifdef HAS_NV21TOARGBROW_NEON
void NV21ToARGBRow_NEON(const uint8* src_y,
                        const uint8* src_uv,
                        uint8* dst_argb,
                        int width) {
  asm volatile (
    MEMACCESS(4)
    "vld1.8     {d24}, [%4]                    \n"
    MEMACCESS(5)
    "vld1.8     {d25}, [%5]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READNV21
    YUV422TORGB
    "subs       %3, %3, #8                     \n"
    "vmov.u8    d23, #255                      \n"
    MEMACCESS(2)
    "vst4.8     {d20, d21, d22, d23}, [%2]!    \n"
    "bgt        1b                             \n"
    : "+r"(src_y),     // %0
      "+r"(src_uv),    // %1
      "+r"(dst_argb),  // %2
      "+r"(width)      // %3
    : "r"(&kUVToRB),   // %4
      "r"(&kUVToG)     // %5
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_NV21TOARGBROW_NEON

#ifdef HAS_NV12TORGB565ROW_NEON
void NV12ToRGB565Row_NEON(const uint8* src_y,
                          const uint8* src_uv,
                          uint8* dst_rgb565,
                          int width) {
  asm volatile (
    MEMACCESS(4)
    "vld1.8     {d24}, [%4]                    \n"
    MEMACCESS(5)
    "vld1.8     {d25}, [%5]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READNV12
    YUV422TORGB
    "subs       %3, %3, #8                     \n"
    ARGBTORGB565
    MEMACCESS(2)
    "vst1.8     {q0}, [%2]!                    \n"  // store 8 pixels RGB565.
    "bgt        1b                             \n"
    : "+r"(src_y),     // %0
      "+r"(src_uv),    // %1
      "+r"(dst_rgb565),  // %2
      "+r"(width)      // %3
    : "r"(&kUVToRB),   // %4
      "r"(&kUVToG)     // %5
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_NV12TORGB565ROW_NEON

#ifdef HAS_NV21TORGB565ROW_NEON
void NV21ToRGB565Row_NEON(const uint8* src_y,
                          const uint8* src_uv,
                          uint8* dst_rgb565,
                          int width) {
  asm volatile (
    MEMACCESS(4)
    "vld1.8     {d24}, [%4]                    \n"
    MEMACCESS(5)
    "vld1.8     {d25}, [%5]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READNV21
    YUV422TORGB
    "subs       %3, %3, #8                     \n"
    ARGBTORGB565
    MEMACCESS(2)
    "vst1.8     {q0}, [%2]!                    \n"  // store 8 pixels RGB565.
    "bgt        1b                             \n"
    : "+r"(src_y),     // %0
      "+r"(src_uv),    // %1
      "+r"(dst_rgb565),  // %2
      "+r"(width)      // %3
    : "r"(&kUVToRB),   // %4
      "r"(&kUVToG)     // %5
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_NV21TORGB565ROW_NEON

#ifdef HAS_YUY2TOARGBROW_NEON
void YUY2ToARGBRow_NEON(const uint8* src_yuy2,
                        uint8* dst_argb,
                        int width) {
  asm volatile (
    MEMACCESS(3)
    "vld1.8     {d24}, [%3]                    \n"
    MEMACCESS(4)
    "vld1.8     {d25}, [%4]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READYUY2
    YUV422TORGB
    "subs       %2, %2, #8                     \n"
    "vmov.u8    d23, #255                      \n"
    MEMACCESS(1)
    "vst4.8     {d20, d21, d22, d23}, [%1]!    \n"
    "bgt        1b                             \n"
    : "+r"(src_yuy2),  // %0
      "+r"(dst_argb),  // %1
      "+r"(width)      // %2
    : "r"(&kUVToRB),   // %3
      "r"(&kUVToG)     // %4
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_YUY2TOARGBROW_NEON

#ifdef HAS_UYVYTOARGBROW_NEON
void UYVYToARGBRow_NEON(const uint8* src_uyvy,
                        uint8* dst_argb,
                        int width) {
  asm volatile (
    MEMACCESS(3)
    "vld1.8     {d24}, [%3]                    \n"
    MEMACCESS(4)
    "vld1.8     {d25}, [%4]                    \n"
    "vmov.u8    d26, #128                      \n"
    "vmov.u16   q14, #74                       \n"
    "vmov.u16   q15, #16                       \n"
    ".p2align   2                              \n"
  "1:                                          \n"
    READUYVY
    YUV422TORGB
    "subs       %2, %2, #8                     \n"
    "vmov.u8    d23, #255                      \n"
    MEMACCESS(1)
    "vst4.8     {d20, d21, d22, d23}, [%1]!    \n"
    "bgt        1b                             \n"
    : "+r"(src_uyvy),  // %0
      "+r"(dst_argb),  // %1
      "+r"(width)      // %2
    : "r"(&kUVToRB),   // %3
      "r"(&kUVToG)     // %4
    : "cc", "memory", "q0", "q1", "q2", "q3",
      "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_UYVYTOARGBROW_NEON

// Reads 16 pairs of UV and write even values to dst_u and odd to dst_v.
#ifdef HAS_SPLITUVROW_NEON
void SplitUVRow_NEON(const uint8* src_uv, uint8* dst_u, uint8* dst_v,
                     int width) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld2        {v0.16b, v1.16b}, [%0], #32    \n"  // load 16 pairs of UV
    "subs       %3, %3, #16                    \n"  // 16 processed per loop
    MEMACCESS(1)
    "st1        {v0.16b}, [%1], #16            \n"  // store U
    MEMACCESS(2)
    "st1        {v1.16b}, [%2], #16            \n"  // store V
    "bgt        1b                             \n"
    : "+r"(src_uv),  // %0
      "+r"(dst_u),   // %1
      "+r"(dst_v),   // %2
      "+r"(width)    // %3  // Output registers
    :                       // Input registers
    : "cc", "memory", "v0", "v1"  // Clobber List
  );
}
#endif  // HAS_SPLITUVROW_NEON

// Reads 16 U's and V's and writes out 16 pairs of UV.
#ifdef HAS_MERGEUVROW_NEON
void MergeUVRow_NEON(const uint8* src_u, const uint8* src_v, uint8* dst_uv,
                     int width) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld1        {v0.16b}, [%0], #16            \n"  // load U
    MEMACCESS(1)
    "ld1        {v1.16b}, [%1], #16            \n"  // load V
    "subs       %3, %3, #16                    \n"  // 16 processed per loop
    MEMACCESS(2)
    "st2        {v0.16b, v1.16b}, [%2], #32    \n"  // store 16 pairs of UV
    "bgt        1b                             \n"
    :
      "+r"(src_u),   // %0
      "+r"(src_v),   // %1
      "+r"(dst_uv),  // %2
      "+r"(width)    // %3  // Output registers
    :                       // Input registers
    : "cc", "memory", "v0", "v1"  // Clobber List
  );
}
#endif  // HAS_MERGEUVROW_NEON

// Copy multiple of 32.  vld4.8  allow unaligned and is fastest on a15.
#ifdef HAS_COPYROW_NEON
void CopyRow_NEON(const uint8* src, uint8* dst, int count) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld1        {v0.8b-v3.8b}, [%0], #32       \n"  // load 32
    "subs       %2, %2, #32                    \n"  // 32 processed per loop
    MEMACCESS(1)
    "st1        {v0.8b-v3.8b}, [%1], #32       \n"  // store 32
    "bgt        1b                             \n"
  : "+r"(src),   // %0
    "+r"(dst),   // %1
    "+r"(count)  // %2  // Output registers
  :                     // Input registers
  : "cc", "memory", "v0", "v1", "v2", "v3"  // Clobber List
  );
}
#endif  // HAS_COPYROW_NEON

// SetRow8 writes 'count' bytes using a 32 bit value repeated.
#ifdef HAS_SETROW_NEON
void SetRow_NEON(uint8* dst, uint32 v32, int count) {
  asm volatile (
    "dup        v0.4s, %w2                     \n"  // duplicate 4 ints
    "1:                                        \n"
    "subs      %1, %1, #16                     \n"  // 16 bytes per loop
    MEMACCESS(0)
    "st1        {v0.16b}, [%0], #16            \n"  // store
    "bgt       1b                              \n"
  : "+r"(dst),   // %0
    "+r"(count)  // %1
  : "r"(v32)     // %2
  : "cc", "memory", "v0"
  );
}
#endif  // HAS_SETROW_NEON

// TODO(fbarchard): Make fully assembler
// SetRow32 writes 'count' words using a 32 bit value repeated.
#ifdef HAS_ARGBSETROWS_NEON
void ARGBSetRows_NEON(uint8* dst, uint32 v32, int width,
                      int dst_stride, int height) {
  for (int y = 0; y < height; ++y) {
    SetRow_NEON(dst, v32, width << 2);
    dst += dst_stride;
  }
}
#endif  // HAS_ARGBSETROWS_NEON

#ifdef HAS_MIRRORROW_NEON
void MirrorRow_NEON(const uint8* src, uint8* dst, int width) {
  asm volatile (
    // Start at end of source row.
    "add        %0, %0, %2                     \n"
    "sub        %0, %0, #16                    \n"

    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld1        {v0.16b}, [%0], %3             \n"  // src -= 16
    "subs       %2, %2, #16                    \n"  // 16 pixels per loop.
    "rev64      v0.16b, v0.16b                 \n"
    MEMACCESS(1)
    "st1        {v0.D}[1], [%1], #8            \n"  // dst += 16
    MEMACCESS(1)
    "st1        {v0.D}[0], [%1], #8            \n"
    "bgt        1b                             \n"
  : "+r"(src),   // %0
    "+r"(dst),   // %1
    "+r"(width)  // %2
  : "r"((ptrdiff_t)-16)    // %3
  : "cc", "memory", "v0"
  );
}
#endif  // HAS_MIRRORROW_NEON

#ifdef HAS_MIRRORUVROW_NEON
void MirrorUVRow_NEON(const uint8* src_uv, uint8* dst_u, uint8* dst_v,
                      int width) {
  asm volatile (
    // Start at end of source row.
    "add        %0, %0, %3, lsl #1             \n"
    "sub        %0, %0, #16                    \n"

    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld2        {v0.8b, v1.8b}, [%0], %4       \n"  // src -= 16
    "subs       %3, %3, #8                     \n"  // 8 pixels per loop.
    "rev64      v0.8b, v0.8b                   \n"
    "rev64      v1.8b, v1.8b                   \n"
    MEMACCESS(1)
    "st1        {v0.8b}, [%1], #8               \n"  // dst += 8
    MEMACCESS(2)
    "st1        {v1.8b}, [%2], #8               \n"
    "bgt        1b                             \n"
  : "+r"(src_uv),  // %0
    "+r"(dst_u),   // %1
    "+r"(dst_v),   // %2
    "+r"(width)    // %3
  : "r"((ptrdiff_t)-16)      // %4
  : "cc", "memory", "v0", "v1"
  );
}
#endif  // HAS_MIRRORUVROW_NEON

#ifdef HAS_ARGBMIRRORROW_NEON
void ARGBMirrorRow_NEON(const uint8* src, uint8* dst, int width) {
  asm volatile (
    // Start at end of source row.
    "add        %0, %0, %2, lsl #2             \n"
    "sub        %0, %0, #16                    \n"

    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld1        {v0.16b}, [%0], %3             \n"  // src -= 16
    "subs       %2, %2, #4                     \n"  // 4 pixels per loop.
    "rev64      v0.4s, v0.4s                   \n"
    MEMACCESS(1)
    "st1        {v0.D}[1], [%1], #8            \n"  // dst += 16
    MEMACCESS(1)
    "st1        {v0.D}[0], [%1], #8            \n"
    "bgt        1b                             \n"
  : "+r"(src),   // %0
    "+r"(dst),   // %1
    "+r"(width)  // %2
  : "r"((ptrdiff_t)-16)    // %3
  : "cc", "memory", "v0"
  );
}
#endif  // HAS_ARGBMIRRORROW_NEON

#ifdef HAS_RGB24TOARGBROW_NEON
void RGB24ToARGBRow_NEON(const uint8* src_rgb24, uint8* dst_argb, int pix) {
  asm volatile (
    "movi       v4.8b, #255                    \n"  // Alpha
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld3        {v1.8b-v3.8b}, [%0], #24       \n"  // load 8 pixels of RGB24.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    MEMACCESS(1)
    "st4        {v1.8b-v4.8b}, [%1], #32       \n"  // store 8 pixels of ARGB.
    "bgt        1b                             \n"
  : "+r"(src_rgb24),  // %0
    "+r"(dst_argb),   // %1
    "+r"(pix)         // %2
  :
  : "cc", "memory", "v1", "v2", "v3", "v4"  // Clobber List
  );
}
#endif  // HAS_RGB24TOARGBROW_NEON

#ifdef HAS_RAWTOARGBROW_NEON
void RAWToARGBRow_NEON(const uint8* src_raw, uint8* dst_argb, int pix) {
  asm volatile (
    "movi       v5.8b, #255                    \n"  // Alpha
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld3        {v0.8b-v2.8b}, [%0], #24       \n"  // read r g b
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "mov        v3.8b, v1.8b                   \n"  // move g
    "mov        v4.8b, v0.8b                   \n"  // move r
    MEMACCESS(1)
    "st4        {v2.8b-v5.8b}, [%1], #32       \n"  // store b g r a
    "bgt        1b                             \n"
  : "+r"(src_raw),   // %0
    "+r"(dst_argb),  // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "v0", "v1", "v2", "v3", "v4", "v5"  // Clobber List
  );
}
#endif  // HAS_RAWTOARGBROW_NEON

#define RGB565TOARGB                                                           \
    "vshrn.u16  d6, q0, #5                     \n"  /* G xxGGGGGG           */ \
    "vuzp.u8    d0, d1                         \n"  /* d0 xxxBBBBB RRRRRxxx */ \
    "vshl.u8    d6, d6, #2                     \n"  /* G GGGGGG00 upper 6   */ \
    "vshr.u8    d1, d1, #3                     \n"  /* R 000RRRRR lower 5   */ \
    "vshl.u8    q0, q0, #3                     \n"  /* B,R BBBBB000 upper 5 */ \
    "vshr.u8    q2, q0, #5                     \n"  /* B,R 00000BBB lower 3 */ \
    "vorr.u8    d0, d0, d4                     \n"  /* B                    */ \
    "vshr.u8    d4, d6, #6                     \n"  /* G 000000GG lower 2   */ \
    "vorr.u8    d2, d1, d5                     \n"  /* R                    */ \
    "vorr.u8    d1, d4, d6                     \n"  /* G                    */

#ifdef HAS_RGB565TOARGBROW_NEON
void RGB565ToARGBRow_NEON(const uint8* src_rgb565, uint8* dst_argb, int pix) {
  asm volatile (
    "vmov.u8    d3, #255                       \n"  // Alpha
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld1.8     {q0}, [%0]!                    \n"  // load 8 RGB565 pixels.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    RGB565TOARGB
    MEMACCESS(1)
    "vst4.8     {d0, d1, d2, d3}, [%1]!        \n"  // store 8 pixels of ARGB.
    "bgt        1b                             \n"
  : "+r"(src_rgb565),  // %0
    "+r"(dst_argb),    // %1
    "+r"(pix)          // %2
  :
  : "cc", "memory", "q0", "q1", "q2", "q3"  // Clobber List
  );
}
#endif  // HAS_RGB565TOARGBROW_NEON

#define ARGB1555TOARGB                                                         \
    "vshrn.u16  d7, q0, #8                     \n"  /* A Arrrrrxx           */ \
    "vshr.u8    d6, d7, #2                     \n"  /* R xxxRRRRR           */ \
    "vshrn.u16  d5, q0, #5                     \n"  /* G xxxGGGGG           */ \
    "vmovn.u16  d4, q0                         \n"  /* B xxxBBBBB           */ \
    "vshr.u8    d7, d7, #7                     \n"  /* A 0000000A           */ \
    "vneg.s8    d7, d7                         \n"  /* A AAAAAAAA upper 8   */ \
    "vshl.u8    d6, d6, #3                     \n"  /* R RRRRR000 upper 5   */ \
    "vshr.u8    q1, q3, #5                     \n"  /* R,A 00000RRR lower 3 */ \
    "vshl.u8    q0, q2, #3                     \n"  /* B,G BBBBB000 upper 5 */ \
    "vshr.u8    q2, q0, #5                     \n"  /* B,G 00000BBB lower 3 */ \
    "vorr.u8    q1, q1, q3                     \n"  /* R,A                  */ \
    "vorr.u8    q0, q0, q2                     \n"  /* B,G                  */ \

// RGB555TOARGB is same as ARGB1555TOARGB but ignores alpha.
#define RGB555TOARGB                                                           \
    "vshrn.u16  d6, q0, #5                     \n"  /* G xxxGGGGG           */ \
    "vuzp.u8    d0, d1                         \n"  /* d0 xxxBBBBB xRRRRRxx */ \
    "vshl.u8    d6, d6, #3                     \n"  /* G GGGGG000 upper 5   */ \
    "vshr.u8    d1, d1, #2                     \n"  /* R 00xRRRRR lower 5   */ \
    "vshl.u8    q0, q0, #3                     \n"  /* B,R BBBBB000 upper 5 */ \
    "vshr.u8    q2, q0, #5                     \n"  /* B,R 00000BBB lower 3 */ \
    "vorr.u8    d0, d0, d4                     \n"  /* B                    */ \
    "vshr.u8    d4, d6, #5                     \n"  /* G 00000GGG lower 3   */ \
    "vorr.u8    d2, d1, d5                     \n"  /* R                    */ \
    "vorr.u8    d1, d4, d6                     \n"  /* G                    */

#ifdef HAS_ARGB1555TOARGBROW_NEON
void ARGB1555ToARGBRow_NEON(const uint8* src_argb1555, uint8* dst_argb,
                            int pix) {
  asm volatile (
    "vmov.u8    d3, #255                       \n"  // Alpha
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld1.8     {q0}, [%0]!                    \n"  // load 8 ARGB1555 pixels.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    ARGB1555TOARGB
    MEMACCESS(1)
    "vst4.8     {d0, d1, d2, d3}, [%1]!        \n"  // store 8 pixels of ARGB.
    "bgt        1b                             \n"
  : "+r"(src_argb1555),  // %0
    "+r"(dst_argb),    // %1
    "+r"(pix)          // %2
  :
  : "cc", "memory", "q0", "q1", "q2", "q3"  // Clobber List
  );
}
#endif  // HAS_ARGB1555TOARGBROW_NEON

#define ARGB4444TOARGB                                                         \
    "vuzp.u8    d0, d1                         \n"  /* d0 BG, d1 RA         */ \
    "vshl.u8    q2, q0, #4                     \n"  /* B,R BBBB0000         */ \
    "vshr.u8    q1, q0, #4                     \n"  /* G,A 0000GGGG         */ \
    "vshr.u8    q0, q2, #4                     \n"  /* B,R 0000BBBB         */ \
    "vorr.u8    q0, q0, q2                     \n"  /* B,R BBBBBBBB         */ \
    "vshl.u8    q2, q1, #4                     \n"  /* G,A GGGG0000         */ \
    "vorr.u8    q1, q1, q2                     \n"  /* G,A GGGGGGGG         */ \
    "vswp.u8    d1, d2                         \n"  /* B,R,G,A -> B,G,R,A   */

#ifdef HAS_ARGB4444TOARGBROW_NEON
void ARGB4444ToARGBRow_NEON(const uint8* src_argb4444, uint8* dst_argb,
                            int pix) {
  asm volatile (
    "vmov.u8    d3, #255                       \n"  // Alpha
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld1.8     {q0}, [%0]!                    \n"  // load 8 ARGB4444 pixels.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    ARGB4444TOARGB
    MEMACCESS(1)
    "vst4.8     {d0, d1, d2, d3}, [%1]!        \n"  // store 8 pixels of ARGB.
    "bgt        1b                             \n"
  : "+r"(src_argb4444),  // %0
    "+r"(dst_argb),    // %1
    "+r"(pix)          // %2
  :
  : "cc", "memory", "q0", "q1", "q2"  // Clobber List
  );
}
#endif  // HAS_ARGB4444TOARGBROW_NEON

#ifdef HAS_ARGBTORGB24ROW_NEON
void ARGBToRGB24Row_NEON(const uint8* src_argb, uint8* dst_rgb24, int pix) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld4        {v1.8b-v4.8b}, [%0], #32       \n"  // load 8 pixels of ARGB.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    MEMACCESS(1)
    "st3        {v1.8b-v3.8b}, [%1], #24       \n"  // store 8 pixels of RGB24.
    "bgt        1b                             \n"
  : "+r"(src_argb),   // %0
    "+r"(dst_rgb24),  // %1
    "+r"(pix)         // %2
  :
  : "cc", "memory", "v1", "v2", "v3", "v4"  // Clobber List
  );
}
#endif  // HAS_ARGBTORGB24ROW_NEON

#ifdef HAS_ARGBTORAWROW_NEON
void ARGBToRAWRow_NEON(const uint8* src_argb, uint8* dst_raw, int pix) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld4        {v1.8b-v4.8b}, [%0], #32       \n"  // load b g r a
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "mov        v4.8b, v2.8b                   \n"  // mov g
    "mov        v5.8b, v1.8b                   \n"  // mov b
    MEMACCESS(1)
    "st3        {v3.8b-v5.8b}, [%1], #24       \n"  // store r g b
    "bgt        1b                             \n"
  : "+r"(src_argb),  // %0
    "+r"(dst_raw),   // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "v1", "v2", "v3", "v4", "v5"  // Clobber List
  );
}
#endif  // HAS_ARGBTORAWROW_NEON

#ifdef HAS_YUY2TOYROW_NEON
void YUY2ToYRow_NEON(const uint8* src_yuy2, uint8* dst_y, int pix) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld2        {v0.16b, v1.16b}, [%0], #32    \n"  // load 16 pixels of YUY2.
    "subs       %2, %2, #16                    \n"  // 16 processed per loop.
    MEMACCESS(1)
    "st1        {v0.16b}, [%1], #16            \n"  // store 16 pixels of Y.
    "bgt        1b                             \n"
  : "+r"(src_yuy2),  // %0
    "+r"(dst_y),     // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "v0", "v1"  // Clobber List
  );
}
#endif  // HAS_YUY2TOYROW_NEON

#ifdef HAS_UYVYTOYROW_NEON
void UYVYToYRow_NEON(const uint8* src_uyvy, uint8* dst_y, int pix) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld2        {v0.16b, v1.16b}, [%0], #32    \n"  // load 16 pixels of UYVY.
    "subs       %2, %2, #16                    \n"  // 16 processed per loop.
    MEMACCESS(1)
    "st1        {v1.16b}, [%1], #16            \n"  // store 16 pixels of Y.
    "bgt        1b                             \n"
  : "+r"(src_uyvy),  // %0
    "+r"(dst_y),     // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "v0", "v1"  // Clobber List
  );
}
#endif  // HAS_UYVYTOYROW_NEON

#ifdef HAS_YUY2TOUV422ROW_NEON
void YUY2ToUV422Row_NEON(const uint8* src_yuy2, uint8* dst_u, uint8* dst_v,
                         int pix) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld4        {v0.8b-v3.8b}, [%0], #32       \n"  // load 16 pixels of YUY2.
    "subs       %3, %3, #16                    \n"  // 16 pixels = 8 UVs.
    MEMACCESS(1)
    "st1        {v1.8b}, [%1], #8              \n"  // store 8 U.
    MEMACCESS(2)
    "st1        {v3.8b}, [%2], #8              \n"  // store 8 V.
    "bgt        1b                             \n"
  : "+r"(src_yuy2),  // %0
    "+r"(dst_u),     // %1
    "+r"(dst_v),     // %2
    "+r"(pix)        // %3
  :
  : "cc", "memory", "v0", "v1", "v2", "v3"  // Clobber List
  );
}
#endif  // HAS_YUY2TOUV422ROW_NEON

#ifdef HAS_UYVYTOUV422ROW_NEON
void UYVYToUV422Row_NEON(const uint8* src_uyvy, uint8* dst_u, uint8* dst_v,
                         int pix) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld4        {v0.8b-v3.8b}, [%0], #32       \n"  // load 16 pixels of UYVY.
    "subs       %3, %3, #16                    \n"  // 16 pixels = 8 UVs.
    MEMACCESS(1)
    "st1        {v0.8b}, [%1], #8              \n"  // store 8 U.
    MEMACCESS(2)
    "st1        {v2.8b}, [%2], #8              \n"  // store 8 V.
    "bgt        1b                             \n"
  : "+r"(src_uyvy),  // %0
    "+r"(dst_u),     // %1
    "+r"(dst_v),     // %2
    "+r"(pix)        // %3
  :
  : "cc", "memory", "v0", "v1", "v2", "v3"  // Clobber List
  );
}
#endif  // HAS_UYVYTOUV422ROW_NEON

#ifdef HAS_YUY2TOUVROW_NEON
void YUY2ToUVRow_NEON(const uint8* src_yuy2, int stride_yuy2,
                      uint8* dst_u, uint8* dst_v, int pix) {
  asm volatile (
    "add        %x1, %x0, %w1, sxtw            \n"  // stride + src_yuy2
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld4        {v0.8b-v3.8b}, [%0], #32       \n"  // load 16 pixels of YUY2.
    "subs       %4, %4, #16                    \n"  // 16 pixels = 8 UVs.
    MEMACCESS(1)
    "ld4        {v4.8b-v7.8b}, [%1], #32       \n"  // load next row YUY2.
    "urhadd     v1.8b, v1.8b, v5.8b            \n"  // average rows of U
    "urhadd     v3.8b, v3.8b, v7.8b            \n"  // average rows of V
    MEMACCESS(2)
    "st1        {v1.8b}, [%2], #8              \n"  // store 8 U.
    MEMACCESS(3)
    "st1        {v3.8b}, [%3], #8              \n"  // store 8 V.
    "bgt        1b                             \n"
  : "+r"(src_yuy2),     // %0
    "+r"(stride_yuy2),  // %1
    "+r"(dst_u),        // %2
    "+r"(dst_v),        // %3
    "+r"(pix)           // %4
  :
  : "cc", "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7"  // Clobber List
  );
}
#endif  // HAS_YUY2TOUVROW_NEON

#ifdef HAS_UYVYTOUVROW_NEON
void UYVYToUVRow_NEON(const uint8* src_uyvy, int stride_uyvy,
                      uint8* dst_u, uint8* dst_v, int pix) {
  asm volatile (
    "add        %x1, %x0, %w1, sxtw            \n"  // stride + src_uyvy
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld4        {v0.8b-v3.8b}, [%0], #32       \n"  // load 16 pixels of UYVY.
    "subs       %4, %4, #16                    \n"  // 16 pixels = 8 UVs.
    MEMACCESS(1)
    "ld4        {v4.8b-v7.8b}, [%1], #32       \n"  // load next row UYVY.
    "urhadd     v0.8b, v0.8b, v4.8b            \n"  // average rows of U
    "urhadd     v2.8b, v2.8b, v6.8b            \n"  // average rows of V
    MEMACCESS(2)
    "st1        {v0.8b}, [%2], #8              \n"  // store 8 U.
    MEMACCESS(3)
    "st1        {v2.8b}, [%3], #8              \n"  // store 8 V.
    "bgt        1b                             \n"
  : "+r"(src_uyvy),     // %0
    "+r"(stride_uyvy),  // %1
    "+r"(dst_u),        // %2
    "+r"(dst_v),        // %3
    "+r"(pix)           // %4
  :
  : "cc", "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7"  // Clobber List
  );
}
#endif  // HAS_UYVYTOUVROW_NEON

#ifdef HAS_HALFROW_NEON
void HalfRow_NEON(const uint8* src_uv, int src_uv_stride,
                  uint8* dst_uv, int pix) {
  asm volatile (
    // change the stride to row 2 pointer
    "add        %x1, %x0, %w1, sxtw            \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld1        {v0.16b}, [%0], #16            \n"  // load row 1 16 pixels.
    "subs       %3, %3, #16                    \n"  // 16 processed per loop
    MEMACCESS(1)
    "ld1        {v1.16b}, [%1], #16            \n"  // load row 2 16 pixels.
    "urhadd     v0.16b, v0.16b, v1.16b         \n"  // average row 1 and 2
    MEMACCESS(2)
    "st1        {v0.16b}, [%2], #16            \n"
    "bgt        1b                             \n"
  : "+r"(src_uv),         // %0
    "+r"(src_uv_stride),  // %1
    "+r"(dst_uv),         // %2
    "+r"(pix)             // %3
  :
  : "cc", "memory", "v0", "v1"  // Clobber List
  );
}
#endif  // HAS_HALFROW_NEON

// Select 2 channels from ARGB on alternating pixels.  e.g.  BGBGBGBG
#ifdef HAS_ARGBTOBAYERROW_NEON
void ARGBToBayerRow_NEON(const uint8* src_argb, uint8* dst_bayer,
                         uint32 selector, int pix) {
  asm volatile (
    "mov        v2.s[0], %w3                   \n"  // selector
  "1:                                          \n"
    MEMACCESS(0)
    "ld1        {v0.16b, v1.16b}, [%0], 32     \n"  // load row 8 pixels.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop
    "tbl        v4.8b, {v0.16b}, v2.8b         \n"  // look up 4 pixels
    "tbl        v5.8b, {v1.16b}, v2.8b         \n"  // look up 4 pixels
    "trn1       v4.4s, v4.4s, v5.4s            \n"  // combine 8 pixels
    MEMACCESS(1)
    "st1        {v4.8b}, [%1], #8              \n"  // store 8.
    "bgt        1b                             \n"
  : "+r"(src_argb),   // %0
    "+r"(dst_bayer),  // %1
    "+r"(pix)         // %2
  : "r"(selector)     // %3
  : "cc", "memory", "v0", "v1", "v2", "v4", "v5"   // Clobber List
  );
}
#endif  // HAS_ARGBTOBAYERROW_NEON

// Select G channels from ARGB.  e.g.  GGGGGGGG
#ifdef HAS_ARGBTOBAYERGGROW_NEON
void ARGBToBayerGGRow_NEON(const uint8* src_argb, uint8* dst_bayer,
                           uint32 /*selector*/, int pix) {
  asm volatile (
  "1:                                          \n"
    MEMACCESS(0)
    "ld4        {v0.8b-v3.8b}, [%0], #32       \n"  // load row 8 pixels.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop
    MEMACCESS(1)
    "st1        {v1.8b}, [%1], #8              \n"  // store 8 G's.
    "bgt        1b                             \n"
  : "+r"(src_argb),   // %0
    "+r"(dst_bayer),  // %1
    "+r"(pix)         // %2
  :
  : "cc", "memory", "v0", "v1", "v2", "v3"  // Clobber List
  );
}
#endif  // HAS_ARGBTOBAYERGGROW_NEON

// For BGRAToARGB, ABGRToARGB, RGBAToARGB, and ARGBToRGBA.
#ifdef HAS_ARGBSHUFFLEROW_NEON
void ARGBShuffleRow_NEON(const uint8* src_argb, uint8* dst_argb,
                         const uint8* shuffler, int pix) {
  asm volatile (
    MEMACCESS(3)
    "ld1        {v2.16b}, [%3]                 \n"  // shuffler
  "1:                                          \n"
    MEMACCESS(0)
    "ld1        {v0.16b}, [%0], #16            \n"  // load 4 pixels.
    "subs       %2, %2, #4                     \n"  // 4 processed per loop
    "tbl        v1.16b, {v0.16b}, v2.16b       \n"  // look up 4 pixels
    MEMACCESS(1)
    "st1        {v1.16b}, [%1], #16            \n"  // store 4.
    "bgt        1b                             \n"
  : "+r"(src_argb),  // %0
    "+r"(dst_argb),  // %1
    "+r"(pix)        // %2
  : "r"(shuffler)    // %3
  : "cc", "memory", "v0", "v1", "v2"  // Clobber List
  );
}
#endif  // HAS_ARGBSHUFFLEROW_NEON

#ifdef HAS_I422TOYUY2ROW_NEON
void I422ToYUY2Row_NEON(const uint8* src_y,
                        const uint8* src_u,
                        const uint8* src_v,
                        uint8* dst_yuy2, int width) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld2        {v0.8b, v1.8b}, [%0], #16      \n"  // load 16 Ys
    "mov        v2.8b, v1.8b                   \n"
    MEMACCESS(1)
    "ld1        {v1.8b}, [%1], #8              \n"  // load 8 Us
    MEMACCESS(2)
    "ld1        {v3.8b}, [%2], #8              \n"  // load 8 Vs
    "subs       %4, %4, #16                    \n"  // 16 pixels
    MEMACCESS(3)
    "st4        {v0.8b-v3.8b}, [%3], #32       \n"  // Store 8 YUY2/16 pixels.
    "bgt        1b                             \n"
  : "+r"(src_y),     // %0
    "+r"(src_u),     // %1
    "+r"(src_v),     // %2
    "+r"(dst_yuy2),  // %3
    "+r"(width)      // %4
  :
  : "cc", "memory", "v0", "v1", "v2", "v3"
  );
}
#endif  // HAS_I422TOYUY2ROW_NEON

#ifdef HAS_I422TOUYVYROW_NEON
void I422ToUYVYRow_NEON(const uint8* src_y,
                        const uint8* src_u,
                        const uint8* src_v,
                        uint8* dst_uyvy, int width) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld2        {v1.8b, v2.8b}, [%0], #16      \n"  // load 16 Ys
    "mov        v3.8b, v2.8b                   \n"
    MEMACCESS(1)
    "ld1        {v0.8b}, [%1], #8              \n"  // load 8 Us
    MEMACCESS(2)
    "ld1        {v2.8b}, [%2], #8              \n"  // load 8 Vs
    "subs       %4, %4, #16                    \n"  // 16 pixels
    MEMACCESS(3)
    "st4        {v0.8b-v3.8b}, [%3], #32       \n"  // Store 8 UYVY/16 pixels.
    "bgt        1b                             \n"
  : "+r"(src_y),     // %0
    "+r"(src_u),     // %1
    "+r"(src_v),     // %2
    "+r"(dst_uyvy),  // %3
    "+r"(width)      // %4
  :
  : "cc", "memory", "v0", "v1", "v2", "v3"
  );
}
#endif  // HAS_I422TOUYVYROW_NEON

#ifdef HAS_ARGBTORGB565ROW_NEON
void ARGBToRGB565Row_NEON(const uint8* src_argb, uint8* dst_rgb565, int pix) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d20, d21, d22, d23}, [%0]!    \n"  // load 8 pixels of ARGB.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    ARGBTORGB565
    MEMACCESS(1)
    "vst1.8     {q0}, [%1]!                    \n"  // store 8 pixels RGB565.
    "bgt        1b                             \n"
  : "+r"(src_argb),  // %0
    "+r"(dst_rgb565),  // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "q0", "q8", "q9", "q10", "q11"
  );
}
#endif  // HAS_ARGBTORGB565ROW_NEON

#ifdef HAS_ARGBTOARGB1555ROW_NEON
void ARGBToARGB1555Row_NEON(const uint8* src_argb, uint8* dst_argb1555,
                            int pix) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d20, d21, d22, d23}, [%0]!    \n"  // load 8 pixels of ARGB.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    ARGBTOARGB1555
    MEMACCESS(1)
    "vst1.8     {q0}, [%1]!                    \n"  // store 8 pixels ARGB1555.
    "bgt        1b                             \n"
  : "+r"(src_argb),  // %0
    "+r"(dst_argb1555),  // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "q0", "q8", "q9", "q10", "q11"
  );
}
#endif  // HAS_ARGBTOARGB1555ROW_NEON

#ifdef HAS_ARGBTOARGB4444ROW_NEON
void ARGBToARGB4444Row_NEON(const uint8* src_argb, uint8* dst_argb4444,
                            int pix) {
  asm volatile (
    "vmov.u8    d4, #0x0f                      \n"  // bits to clear with vbic.
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d20, d21, d22, d23}, [%0]!    \n"  // load 8 pixels of ARGB.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    ARGBTOARGB4444
    MEMACCESS(1)
    "vst1.8     {q0}, [%1]!                    \n"  // store 8 pixels ARGB4444.
    "bgt        1b                             \n"
  : "+r"(src_argb),      // %0
    "+r"(dst_argb4444),  // %1
    "+r"(pix)            // %2
  :
  : "cc", "memory", "q0", "q8", "q9", "q10", "q11"
  );
}
#endif  // HAS_ARGBTOARGB4444ROW_NEON

#ifdef HAS_ARGBTOYROW_NEON
void ARGBToYRow_NEON(const uint8* src_argb, uint8* dst_y, int pix) {
  asm volatile (
    "movi       v4.8b, #13                     \n"  // B * 0.1016 coefficient
    "movi       v5.8b, #65                     \n"  // G * 0.5078 coefficient
    "movi       v6.8b, #33                     \n"  // R * 0.2578 coefficient
    "movi       v7.8b, #16                     \n"  // Add 16 constant
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld4        {v0.8b-v3.8b}, [%0], #32       \n"  // load 8 ARGB pixels.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "umull      v3.8h, v0.8b, v4.8b            \n"  // B
    "umlal      v3.8h, v1.8b, v5.8b            \n"  // G
    "umlal      v3.8h, v2.8b, v6.8b            \n"  // R
    "sqrshrun   v0.8b, v3.8h, #7               \n"  // 16 bit to 8 bit Y
    "uqadd      v0.8b, v0.8b, v7.8b            \n"
    MEMACCESS(1)
    "st1        {v0.8b}, [%1], #8              \n"  // store 8 pixels Y.
    "bgt        1b                             \n"
  : "+r"(src_argb),  // %0
    "+r"(dst_y),     // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7"
  );
}
#endif  // HAS_ARGBTOYROW_NEON

#ifdef HAS_ARGBTOYJROW_NEON
void ARGBToYJRow_NEON(const uint8* src_argb, uint8* dst_y, int pix) {
  asm volatile (
    "movi       v4.8b, #15                     \n"  // B * 0.11400 coefficient
    "movi       v5.8b, #75                     \n"  // G * 0.58700 coefficient
    "movi       v6.8b, #38                     \n"  // R * 0.29900 coefficient
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld4        {v0.8b-v3.8b}, [%0], #32       \n"  // load 8 ARGB pixels.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "umull      v3.8h, v0.8b, v4.8b            \n"  // B
    "umlal      v3.8h, v1.8b, v5.8b            \n"  // G
    "umlal      v3.8h, v2.8b, v6.8b            \n"  // R
    "sqrshrun   v0.8b, v3.8h, #7               \n"  // 15 bit to 8 bit Y
    MEMACCESS(1)
    "st1        {v0.8b}, [%1], #8              \n"  // store 8 pixels Y.
    "bgt        1b                             \n"
  : "+r"(src_argb),  // %0
    "+r"(dst_y),     // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6"
  );
}
#endif  // HAS_ARGBTOYJROW_NEON

// 8x1 pixels.
#ifdef HAS_ARGBTOUV444ROW_NEON
void ARGBToUV444Row_NEON(const uint8* src_argb, uint8* dst_u, uint8* dst_v,
                         int pix) {
  asm volatile (
    "vmov.u8    d24, #112                      \n"  // UB / VR 0.875 coefficient
    "vmov.u8    d25, #74                       \n"  // UG -0.5781 coefficient
    "vmov.u8    d26, #38                       \n"  // UR -0.2969 coefficient
    "vmov.u8    d27, #18                       \n"  // VB -0.1406 coefficient
    "vmov.u8    d28, #94                       \n"  // VG -0.7344 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d1, d2, d3}, [%0]!        \n"  // load 8 ARGB pixels.
    "subs       %3, %3, #8                     \n"  // 8 processed per loop.
    "vmull.u8   q2, d0, d24                    \n"  // B
    "vmlsl.u8   q2, d1, d25                    \n"  // G
    "vmlsl.u8   q2, d2, d26                    \n"  // R
    "vadd.u16   q2, q2, q15                    \n"  // +128 -> unsigned

    "vmull.u8   q3, d2, d24                    \n"  // R
    "vmlsl.u8   q3, d1, d28                    \n"  // G
    "vmlsl.u8   q3, d0, d27                    \n"  // B
    "vadd.u16   q3, q3, q15                    \n"  // +128 -> unsigned

    "vqshrn.u16  d0, q2, #8                    \n"  // 16 bit to 8 bit U
    "vqshrn.u16  d1, q3, #8                    \n"  // 16 bit to 8 bit V

    MEMACCESS(1)
    "vst1.8     {d0}, [%1]!                    \n"  // store 8 pixels U.
    MEMACCESS(2)
    "vst1.8     {d1}, [%2]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_argb),  // %0
    "+r"(dst_u),     // %1
    "+r"(dst_v),     // %2
    "+r"(pix)        // %3
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_ARGBTOUV444ROW_NEON

// 16x1 pixels -> 8x1.  pix is number of argb pixels. e.g. 16.
#ifdef HAS_ARGBTOUV422ROW_NEON
void ARGBToUV422Row_NEON(const uint8* src_argb, uint8* dst_u, uint8* dst_v,
                         int pix) {
  asm volatile (
    "vmov.s16   q10, #112 / 2                  \n"  // UB / VR 0.875 coefficient
    "vmov.s16   q11, #74 / 2                   \n"  // UG -0.5781 coefficient
    "vmov.s16   q12, #38 / 2                   \n"  // UR -0.2969 coefficient
    "vmov.s16   q13, #18 / 2                   \n"  // VB -0.1406 coefficient
    "vmov.s16   q14, #94 / 2                   \n"  // VG -0.7344 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d2, d4, d6}, [%0]!        \n"  // load 8 ARGB pixels.
    MEMACCESS(0)
    "vld4.8     {d1, d3, d5, d7}, [%0]!        \n"  // load next 8 ARGB pixels.

    "vpaddl.u8  q0, q0                         \n"  // B 16 bytes -> 8 shorts.
    "vpaddl.u8  q1, q1                         \n"  // G 16 bytes -> 8 shorts.
    "vpaddl.u8  q2, q2                         \n"  // R 16 bytes -> 8 shorts.

    "subs       %3, %3, #16                    \n"  // 16 processed per loop.
    "vmul.s16   q8, q0, q10                    \n"  // B
    "vmls.s16   q8, q1, q11                    \n"  // G
    "vmls.s16   q8, q2, q12                    \n"  // R
    "vadd.u16   q8, q8, q15                    \n"  // +128 -> unsigned

    "vmul.s16   q9, q2, q10                    \n"  // R
    "vmls.s16   q9, q1, q14                    \n"  // G
    "vmls.s16   q9, q0, q13                    \n"  // B
    "vadd.u16   q9, q9, q15                    \n"  // +128 -> unsigned

    "vqshrn.u16  d0, q8, #8                    \n"  // 16 bit to 8 bit U
    "vqshrn.u16  d1, q9, #8                    \n"  // 16 bit to 8 bit V

    MEMACCESS(1)
    "vst1.8     {d0}, [%1]!                    \n"  // store 8 pixels U.
    MEMACCESS(2)
    "vst1.8     {d1}, [%2]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_argb),  // %0
    "+r"(dst_u),     // %1
    "+r"(dst_v),     // %2
    "+r"(pix)        // %3
  :
  : "cc", "memory", "q0", "q1", "q2", "q3",
    "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_ARGBTOUV422ROW_NEON

// 32x1 pixels -> 8x1.  pix is number of argb pixels. e.g. 32.
#ifdef HAS_ARGBTOUV411ROW_NEON
void ARGBToUV411Row_NEON(const uint8* src_argb, uint8* dst_u, uint8* dst_v,
                         int pix) {
  asm volatile (
    "vmov.s16   q10, #112 / 2                  \n"  // UB / VR 0.875 coefficient
    "vmov.s16   q11, #74 / 2                   \n"  // UG -0.5781 coefficient
    "vmov.s16   q12, #38 / 2                   \n"  // UR -0.2969 coefficient
    "vmov.s16   q13, #18 / 2                   \n"  // VB -0.1406 coefficient
    "vmov.s16   q14, #94 / 2                   \n"  // VG -0.7344 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d2, d4, d6}, [%0]!        \n"  // load 8 ARGB pixels.
    MEMACCESS(0)
    "vld4.8     {d1, d3, d5, d7}, [%0]!        \n"  // load next 8 ARGB pixels.
    "vpaddl.u8  q0, q0                         \n"  // B 16 bytes -> 8 shorts.
    "vpaddl.u8  q1, q1                         \n"  // G 16 bytes -> 8 shorts.
    "vpaddl.u8  q2, q2                         \n"  // R 16 bytes -> 8 shorts.
    MEMACCESS(0)
    "vld4.8     {d8, d10, d12, d14}, [%0]!     \n"  // load 8 more ARGB pixels.
    MEMACCESS(0)
    "vld4.8     {d9, d11, d13, d15}, [%0]!     \n"  // load last 8 ARGB pixels.
    "vpaddl.u8  q4, q4                         \n"  // B 16 bytes -> 8 shorts.
    "vpaddl.u8  q5, q5                         \n"  // G 16 bytes -> 8 shorts.
    "vpaddl.u8  q6, q6                         \n"  // R 16 bytes -> 8 shorts.

    "vpadd.u16  d0, d0, d1                     \n"  // B 16 shorts -> 8 shorts.
    "vpadd.u16  d1, d8, d9                     \n"  // B
    "vpadd.u16  d2, d2, d3                     \n"  // G 16 shorts -> 8 shorts.
    "vpadd.u16  d3, d10, d11                   \n"  // G
    "vpadd.u16  d4, d4, d5                     \n"  // R 16 shorts -> 8 shorts.
    "vpadd.u16  d5, d12, d13                   \n"  // R

    "vrshr.u16  q0, q0, #1                     \n"  // 2x average
    "vrshr.u16  q1, q1, #1                     \n"
    "vrshr.u16  q2, q2, #1                     \n"

    "subs       %3, %3, #32                    \n"  // 32 processed per loop.
    "vmul.s16   q8, q0, q10                    \n"  // B
    "vmls.s16   q8, q1, q11                    \n"  // G
    "vmls.s16   q8, q2, q12                    \n"  // R
    "vadd.u16   q8, q8, q15                    \n"  // +128 -> unsigned
    "vmul.s16   q9, q2, q10                    \n"  // R
    "vmls.s16   q9, q1, q14                    \n"  // G
    "vmls.s16   q9, q0, q13                    \n"  // B
    "vadd.u16   q9, q9, q15                    \n"  // +128 -> unsigned
    "vqshrn.u16  d0, q8, #8                    \n"  // 16 bit to 8 bit U
    "vqshrn.u16  d1, q9, #8                    \n"  // 16 bit to 8 bit V
    MEMACCESS(1)
    "vst1.8     {d0}, [%1]!                    \n"  // store 8 pixels U.
    MEMACCESS(2)
    "vst1.8     {d1}, [%2]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_argb),  // %0
    "+r"(dst_u),     // %1
    "+r"(dst_v),     // %2
    "+r"(pix)        // %3
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7",
    "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_ARGBTOUV411ROW_NEON

// 16x2 pixels -> 8x1.  pix is number of argb pixels. e.g. 16.
#define RGBTOUV(QB, QG, QR) \
    "vmul.s16   q8, " #QB ", q10               \n"  /* B                    */ \
    "vmls.s16   q8, " #QG ", q11               \n"  /* G                    */ \
    "vmls.s16   q8, " #QR ", q12               \n"  /* R                    */ \
    "vadd.u16   q8, q8, q15                    \n"  /* +128 -> unsigned     */ \
    "vmul.s16   q9, " #QR ", q10               \n"  /* R                    */ \
    "vmls.s16   q9, " #QG ", q14               \n"  /* G                    */ \
    "vmls.s16   q9, " #QB ", q13               \n"  /* B                    */ \
    "vadd.u16   q9, q9, q15                    \n"  /* +128 -> unsigned     */ \
    "vqshrn.u16  d0, q8, #8                    \n"  /* 16 bit to 8 bit U    */ \
    "vqshrn.u16  d1, q9, #8                    \n"  /* 16 bit to 8 bit V    */

// TODO(fbarchard): Consider vhadd vertical, then vpaddl horizontal, avoid shr.
#ifdef HAS_ARGBTOUVROW_NEON
void ARGBToUVRow_NEON(const uint8* src_argb, int src_stride_argb,
                      uint8* dst_u, uint8* dst_v, int pix) {
  asm volatile (
    "add        %1, %0, %1                     \n"  // src_stride + src_argb
    "vmov.s16   q10, #112 / 2                  \n"  // UB / VR 0.875 coefficient
    "vmov.s16   q11, #74 / 2                   \n"  // UG -0.5781 coefficient
    "vmov.s16   q12, #38 / 2                   \n"  // UR -0.2969 coefficient
    "vmov.s16   q13, #18 / 2                   \n"  // VB -0.1406 coefficient
    "vmov.s16   q14, #94 / 2                   \n"  // VG -0.7344 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d2, d4, d6}, [%0]!        \n"  // load 8 ARGB pixels.
    MEMACCESS(0)
    "vld4.8     {d1, d3, d5, d7}, [%0]!        \n"  // load next 8 ARGB pixels.
    "vpaddl.u8  q0, q0                         \n"  // B 16 bytes -> 8 shorts.
    "vpaddl.u8  q1, q1                         \n"  // G 16 bytes -> 8 shorts.
    "vpaddl.u8  q2, q2                         \n"  // R 16 bytes -> 8 shorts.
    MEMACCESS(1)
    "vld4.8     {d8, d10, d12, d14}, [%1]!     \n"  // load 8 more ARGB pixels.
    MEMACCESS(1)
    "vld4.8     {d9, d11, d13, d15}, [%1]!     \n"  // load last 8 ARGB pixels.
    "vpadal.u8  q0, q4                         \n"  // B 16 bytes -> 8 shorts.
    "vpadal.u8  q1, q5                         \n"  // G 16 bytes -> 8 shorts.
    "vpadal.u8  q2, q6                         \n"  // R 16 bytes -> 8 shorts.

    "vrshr.u16  q0, q0, #1                     \n"  // 2x average
    "vrshr.u16  q1, q1, #1                     \n"
    "vrshr.u16  q2, q2, #1                     \n"

    "subs       %4, %4, #16                    \n"  // 32 processed per loop.
    RGBTOUV(q0, q1, q2)
    MEMACCESS(2)
    "vst1.8     {d0}, [%2]!                    \n"  // store 8 pixels U.
    MEMACCESS(3)
    "vst1.8     {d1}, [%3]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_argb),  // %0
    "+r"(src_stride_argb),  // %1
    "+r"(dst_u),     // %2
    "+r"(dst_v),     // %3
    "+r"(pix)        // %4
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7",
    "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_ARGBTOUVROW_NEON

// TODO(fbarchard): Subsample match C code.
#ifdef HAS_ARGBTOUVJROW_NEON
void ARGBToUVJRow_NEON(const uint8* src_argb, int src_stride_argb,
                       uint8* dst_u, uint8* dst_v, int pix) {
  asm volatile (
    "add        %1, %0, %1                     \n"  // src_stride + src_argb
    "vmov.s16   q10, #127 / 2                  \n"  // UB / VR 0.500 coefficient
    "vmov.s16   q11, #84 / 2                   \n"  // UG -0.33126 coefficient
    "vmov.s16   q12, #43 / 2                   \n"  // UR -0.16874 coefficient
    "vmov.s16   q13, #20 / 2                   \n"  // VB -0.08131 coefficient
    "vmov.s16   q14, #107 / 2                  \n"  // VG -0.41869 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d2, d4, d6}, [%0]!        \n"  // load 8 ARGB pixels.
    MEMACCESS(0)
    "vld4.8     {d1, d3, d5, d7}, [%0]!        \n"  // load next 8 ARGB pixels.
    "vpaddl.u8  q0, q0                         \n"  // B 16 bytes -> 8 shorts.
    "vpaddl.u8  q1, q1                         \n"  // G 16 bytes -> 8 shorts.
    "vpaddl.u8  q2, q2                         \n"  // R 16 bytes -> 8 shorts.
    MEMACCESS(1)
    "vld4.8     {d8, d10, d12, d14}, [%1]!     \n"  // load 8 more ARGB pixels.
    MEMACCESS(1)
    "vld4.8     {d9, d11, d13, d15}, [%1]!     \n"  // load last 8 ARGB pixels.
    "vpadal.u8  q0, q4                         \n"  // B 16 bytes -> 8 shorts.
    "vpadal.u8  q1, q5                         \n"  // G 16 bytes -> 8 shorts.
    "vpadal.u8  q2, q6                         \n"  // R 16 bytes -> 8 shorts.

    "vrshr.u16  q0, q0, #1                     \n"  // 2x average
    "vrshr.u16  q1, q1, #1                     \n"
    "vrshr.u16  q2, q2, #1                     \n"

    "subs       %4, %4, #16                    \n"  // 32 processed per loop.
    RGBTOUV(q0, q1, q2)
    MEMACCESS(2)
    "vst1.8     {d0}, [%2]!                    \n"  // store 8 pixels U.
    MEMACCESS(3)
    "vst1.8     {d1}, [%3]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_argb),  // %0
    "+r"(src_stride_argb),  // %1
    "+r"(dst_u),     // %2
    "+r"(dst_v),     // %3
    "+r"(pix)        // %4
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7",
    "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_ARGBTOUVJROW_NEON

#ifdef HAS_BGRATOUVROW_NEON
void BGRAToUVRow_NEON(const uint8* src_bgra, int src_stride_bgra,
                      uint8* dst_u, uint8* dst_v, int pix) {
  asm volatile (
    "add        %1, %0, %1                     \n"  // src_stride + src_bgra
    "vmov.s16   q10, #112 / 2                  \n"  // UB / VR 0.875 coefficient
    "vmov.s16   q11, #74 / 2                   \n"  // UG -0.5781 coefficient
    "vmov.s16   q12, #38 / 2                   \n"  // UR -0.2969 coefficient
    "vmov.s16   q13, #18 / 2                   \n"  // VB -0.1406 coefficient
    "vmov.s16   q14, #94 / 2                   \n"  // VG -0.7344 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d2, d4, d6}, [%0]!        \n"  // load 8 BGRA pixels.
    MEMACCESS(0)
    "vld4.8     {d1, d3, d5, d7}, [%0]!        \n"  // load next 8 BGRA pixels.
    "vpaddl.u8  q3, q3                         \n"  // B 16 bytes -> 8 shorts.
    "vpaddl.u8  q2, q2                         \n"  // G 16 bytes -> 8 shorts.
    "vpaddl.u8  q1, q1                         \n"  // R 16 bytes -> 8 shorts.
    MEMACCESS(1)
    "vld4.8     {d8, d10, d12, d14}, [%1]!     \n"  // load 8 more BGRA pixels.
    MEMACCESS(1)
    "vld4.8     {d9, d11, d13, d15}, [%1]!     \n"  // load last 8 BGRA pixels.
    "vpadal.u8  q3, q7                         \n"  // B 16 bytes -> 8 shorts.
    "vpadal.u8  q2, q6                         \n"  // G 16 bytes -> 8 shorts.
    "vpadal.u8  q1, q5                         \n"  // R 16 bytes -> 8 shorts.

    "vrshr.u16  q1, q1, #1                     \n"  // 2x average
    "vrshr.u16  q2, q2, #1                     \n"
    "vrshr.u16  q3, q3, #1                     \n"

    "subs       %4, %4, #16                    \n"  // 32 processed per loop.
    RGBTOUV(q3, q2, q1)
    MEMACCESS(2)
    "vst1.8     {d0}, [%2]!                    \n"  // store 8 pixels U.
    MEMACCESS(3)
    "vst1.8     {d1}, [%3]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_bgra),  // %0
    "+r"(src_stride_bgra),  // %1
    "+r"(dst_u),     // %2
    "+r"(dst_v),     // %3
    "+r"(pix)        // %4
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7",
    "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_BGRATOUVROW_NEON

#ifdef HAS_ABGRTOUVROW_NEON
void ABGRToUVRow_NEON(const uint8* src_abgr, int src_stride_abgr,
                      uint8* dst_u, uint8* dst_v, int pix) {
  asm volatile (
    "add        %1, %0, %1                     \n"  // src_stride + src_abgr
    "vmov.s16   q10, #112 / 2                  \n"  // UB / VR 0.875 coefficient
    "vmov.s16   q11, #74 / 2                   \n"  // UG -0.5781 coefficient
    "vmov.s16   q12, #38 / 2                   \n"  // UR -0.2969 coefficient
    "vmov.s16   q13, #18 / 2                   \n"  // VB -0.1406 coefficient
    "vmov.s16   q14, #94 / 2                   \n"  // VG -0.7344 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d2, d4, d6}, [%0]!        \n"  // load 8 ABGR pixels.
    MEMACCESS(0)
    "vld4.8     {d1, d3, d5, d7}, [%0]!        \n"  // load next 8 ABGR pixels.
    "vpaddl.u8  q2, q2                         \n"  // B 16 bytes -> 8 shorts.
    "vpaddl.u8  q1, q1                         \n"  // G 16 bytes -> 8 shorts.
    "vpaddl.u8  q0, q0                         \n"  // R 16 bytes -> 8 shorts.
    MEMACCESS(1)
    "vld4.8     {d8, d10, d12, d14}, [%1]!     \n"  // load 8 more ABGR pixels.
    MEMACCESS(1)
    "vld4.8     {d9, d11, d13, d15}, [%1]!     \n"  // load last 8 ABGR pixels.
    "vpadal.u8  q2, q6                         \n"  // B 16 bytes -> 8 shorts.
    "vpadal.u8  q1, q5                         \n"  // G 16 bytes -> 8 shorts.
    "vpadal.u8  q0, q4                         \n"  // R 16 bytes -> 8 shorts.

    "vrshr.u16  q0, q0, #1                     \n"  // 2x average
    "vrshr.u16  q1, q1, #1                     \n"
    "vrshr.u16  q2, q2, #1                     \n"

    "subs       %4, %4, #16                    \n"  // 32 processed per loop.
    RGBTOUV(q2, q1, q0)
    MEMACCESS(2)
    "vst1.8     {d0}, [%2]!                    \n"  // store 8 pixels U.
    MEMACCESS(3)
    "vst1.8     {d1}, [%3]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_abgr),  // %0
    "+r"(src_stride_abgr),  // %1
    "+r"(dst_u),     // %2
    "+r"(dst_v),     // %3
    "+r"(pix)        // %4
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7",
    "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_ABGRTOUVROW_NEON

#ifdef HAS_RGBATOUVROW_NEON
void RGBAToUVRow_NEON(const uint8* src_rgba, int src_stride_rgba,
                      uint8* dst_u, uint8* dst_v, int pix) {
  asm volatile (
    "add        %1, %0, %1                     \n"  // src_stride + src_rgba
    "vmov.s16   q10, #112 / 2                  \n"  // UB / VR 0.875 coefficient
    "vmov.s16   q11, #74 / 2                   \n"  // UG -0.5781 coefficient
    "vmov.s16   q12, #38 / 2                   \n"  // UR -0.2969 coefficient
    "vmov.s16   q13, #18 / 2                   \n"  // VB -0.1406 coefficient
    "vmov.s16   q14, #94 / 2                   \n"  // VG -0.7344 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d2, d4, d6}, [%0]!        \n"  // load 8 RGBA pixels.
    MEMACCESS(0)
    "vld4.8     {d1, d3, d5, d7}, [%0]!        \n"  // load next 8 RGBA pixels.
    "vpaddl.u8  q0, q1                         \n"  // B 16 bytes -> 8 shorts.
    "vpaddl.u8  q1, q2                         \n"  // G 16 bytes -> 8 shorts.
    "vpaddl.u8  q2, q3                         \n"  // R 16 bytes -> 8 shorts.
    MEMACCESS(1)
    "vld4.8     {d8, d10, d12, d14}, [%1]!     \n"  // load 8 more RGBA pixels.
    MEMACCESS(1)
    "vld4.8     {d9, d11, d13, d15}, [%1]!     \n"  // load last 8 RGBA pixels.
    "vpadal.u8  q0, q5                         \n"  // B 16 bytes -> 8 shorts.
    "vpadal.u8  q1, q6                         \n"  // G 16 bytes -> 8 shorts.
    "vpadal.u8  q2, q7                         \n"  // R 16 bytes -> 8 shorts.

    "vrshr.u16  q0, q0, #1                     \n"  // 2x average
    "vrshr.u16  q1, q1, #1                     \n"
    "vrshr.u16  q2, q2, #1                     \n"

    "subs       %4, %4, #16                    \n"  // 32 processed per loop.
    RGBTOUV(q0, q1, q2)
    MEMACCESS(2)
    "vst1.8     {d0}, [%2]!                    \n"  // store 8 pixels U.
    MEMACCESS(3)
    "vst1.8     {d1}, [%3]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_rgba),  // %0
    "+r"(src_stride_rgba),  // %1
    "+r"(dst_u),     // %2
    "+r"(dst_v),     // %3
    "+r"(pix)        // %4
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7",
    "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_RGBATOUVROW_NEON

#ifdef HAS_RGB24TOUVROW_NEON
void RGB24ToUVRow_NEON(const uint8* src_rgb24, int src_stride_rgb24,
                       uint8* dst_u, uint8* dst_v, int pix) {
  asm volatile (
    "add        %1, %0, %1                     \n"  // src_stride + src_rgb24
    "vmov.s16   q10, #112 / 2                  \n"  // UB / VR 0.875 coefficient
    "vmov.s16   q11, #74 / 2                   \n"  // UG -0.5781 coefficient
    "vmov.s16   q12, #38 / 2                   \n"  // UR -0.2969 coefficient
    "vmov.s16   q13, #18 / 2                   \n"  // VB -0.1406 coefficient
    "vmov.s16   q14, #94 / 2                   \n"  // VG -0.7344 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld3.8     {d0, d2, d4}, [%0]!            \n"  // load 8 RGB24 pixels.
    MEMACCESS(0)
    "vld3.8     {d1, d3, d5}, [%0]!            \n"  // load next 8 RGB24 pixels.
    "vpaddl.u8  q0, q0                         \n"  // B 16 bytes -> 8 shorts.
    "vpaddl.u8  q1, q1                         \n"  // G 16 bytes -> 8 shorts.
    "vpaddl.u8  q2, q2                         \n"  // R 16 bytes -> 8 shorts.
    MEMACCESS(1)
    "vld3.8     {d8, d10, d12}, [%1]!          \n"  // load 8 more RGB24 pixels.
    MEMACCESS(1)
    "vld3.8     {d9, d11, d13}, [%1]!          \n"  // load last 8 RGB24 pixels.
    "vpadal.u8  q0, q4                         \n"  // B 16 bytes -> 8 shorts.
    "vpadal.u8  q1, q5                         \n"  // G 16 bytes -> 8 shorts.
    "vpadal.u8  q2, q6                         \n"  // R 16 bytes -> 8 shorts.

    "vrshr.u16  q0, q0, #1                     \n"  // 2x average
    "vrshr.u16  q1, q1, #1                     \n"
    "vrshr.u16  q2, q2, #1                     \n"

    "subs       %4, %4, #16                    \n"  // 32 processed per loop.
    RGBTOUV(q0, q1, q2)
    MEMACCESS(2)
    "vst1.8     {d0}, [%2]!                    \n"  // store 8 pixels U.
    MEMACCESS(3)
    "vst1.8     {d1}, [%3]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_rgb24),  // %0
    "+r"(src_stride_rgb24),  // %1
    "+r"(dst_u),     // %2
    "+r"(dst_v),     // %3
    "+r"(pix)        // %4
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7",
    "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_RGB24TOUVROW_NEON

#ifdef HAS_RAWTOUVROW_NEON
void RAWToUVRow_NEON(const uint8* src_raw, int src_stride_raw,
                     uint8* dst_u, uint8* dst_v, int pix) {
  asm volatile (
    "add        %1, %0, %1                     \n"  // src_stride + src_raw
    "vmov.s16   q10, #112 / 2                  \n"  // UB / VR 0.875 coefficient
    "vmov.s16   q11, #74 / 2                   \n"  // UG -0.5781 coefficient
    "vmov.s16   q12, #38 / 2                   \n"  // UR -0.2969 coefficient
    "vmov.s16   q13, #18 / 2                   \n"  // VB -0.1406 coefficient
    "vmov.s16   q14, #94 / 2                   \n"  // VG -0.7344 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld3.8     {d0, d2, d4}, [%0]!            \n"  // load 8 RAW pixels.
    MEMACCESS(0)
    "vld3.8     {d1, d3, d5}, [%0]!            \n"  // load next 8 RAW pixels.
    "vpaddl.u8  q2, q2                         \n"  // B 16 bytes -> 8 shorts.
    "vpaddl.u8  q1, q1                         \n"  // G 16 bytes -> 8 shorts.
    "vpaddl.u8  q0, q0                         \n"  // R 16 bytes -> 8 shorts.
    MEMACCESS(1)
    "vld3.8     {d8, d10, d12}, [%1]!          \n"  // load 8 more RAW pixels.
    MEMACCESS(1)
    "vld3.8     {d9, d11, d13}, [%1]!          \n"  // load last 8 RAW pixels.
    "vpadal.u8  q2, q6                         \n"  // B 16 bytes -> 8 shorts.
    "vpadal.u8  q1, q5                         \n"  // G 16 bytes -> 8 shorts.
    "vpadal.u8  q0, q4                         \n"  // R 16 bytes -> 8 shorts.

    "vrshr.u16  q0, q0, #1                     \n"  // 2x average
    "vrshr.u16  q1, q1, #1                     \n"
    "vrshr.u16  q2, q2, #1                     \n"

    "subs       %4, %4, #16                    \n"  // 32 processed per loop.
    RGBTOUV(q2, q1, q0)
    MEMACCESS(2)
    "vst1.8     {d0}, [%2]!                    \n"  // store 8 pixels U.
    MEMACCESS(3)
    "vst1.8     {d1}, [%3]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_raw),  // %0
    "+r"(src_stride_raw),  // %1
    "+r"(dst_u),     // %2
    "+r"(dst_v),     // %3
    "+r"(pix)        // %4
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7",
    "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_RAWTOUVROW_NEON

// 16x2 pixels -> 8x1.  pix is number of argb pixels. e.g. 16.
#ifdef HAS_RGB565TOUVROW_NEON
void RGB565ToUVRow_NEON(const uint8* src_rgb565, int src_stride_rgb565,
                        uint8* dst_u, uint8* dst_v, int pix) {
  asm volatile (
    "add        %1, %0, %1                     \n"  // src_stride + src_argb
    "vmov.s16   q10, #112 / 2                  \n"  // UB / VR 0.875 coefficient
    "vmov.s16   q11, #74 / 2                   \n"  // UG -0.5781 coefficient
    "vmov.s16   q12, #38 / 2                   \n"  // UR -0.2969 coefficient
    "vmov.s16   q13, #18 / 2                   \n"  // VB -0.1406 coefficient
    "vmov.s16   q14, #94 / 2                   \n"  // VG -0.7344 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld1.8     {q0}, [%0]!                    \n"  // load 8 RGB565 pixels.
    RGB565TOARGB
    "vpaddl.u8  d8, d0                         \n"  // B 8 bytes -> 4 shorts.
    "vpaddl.u8  d10, d1                        \n"  // G 8 bytes -> 4 shorts.
    "vpaddl.u8  d12, d2                        \n"  // R 8 bytes -> 4 shorts.
    MEMACCESS(0)
    "vld1.8     {q0}, [%0]!                    \n"  // next 8 RGB565 pixels.
    RGB565TOARGB
    "vpaddl.u8  d9, d0                         \n"  // B 8 bytes -> 4 shorts.
    "vpaddl.u8  d11, d1                        \n"  // G 8 bytes -> 4 shorts.
    "vpaddl.u8  d13, d2                        \n"  // R 8 bytes -> 4 shorts.

    MEMACCESS(1)
    "vld1.8     {q0}, [%1]!                    \n"  // load 8 RGB565 pixels.
    RGB565TOARGB
    "vpadal.u8  d8, d0                         \n"  // B 8 bytes -> 4 shorts.
    "vpadal.u8  d10, d1                        \n"  // G 8 bytes -> 4 shorts.
    "vpadal.u8  d12, d2                        \n"  // R 8 bytes -> 4 shorts.
    MEMACCESS(1)
    "vld1.8     {q0}, [%1]!                    \n"  // next 8 RGB565 pixels.
    RGB565TOARGB
    "vpadal.u8  d9, d0                         \n"  // B 8 bytes -> 4 shorts.
    "vpadal.u8  d11, d1                        \n"  // G 8 bytes -> 4 shorts.
    "vpadal.u8  d13, d2                        \n"  // R 8 bytes -> 4 shorts.

    "vrshr.u16  q4, q4, #1                     \n"  // 2x average
    "vrshr.u16  q5, q5, #1                     \n"
    "vrshr.u16  q6, q6, #1                     \n"

    "subs       %4, %4, #16                    \n"  // 16 processed per loop.
    "vmul.s16   q8, q4, q10                    \n"  // B
    "vmls.s16   q8, q5, q11                    \n"  // G
    "vmls.s16   q8, q6, q12                    \n"  // R
    "vadd.u16   q8, q8, q15                    \n"  // +128 -> unsigned
    "vmul.s16   q9, q6, q10                    \n"  // R
    "vmls.s16   q9, q5, q14                    \n"  // G
    "vmls.s16   q9, q4, q13                    \n"  // B
    "vadd.u16   q9, q9, q15                    \n"  // +128 -> unsigned
    "vqshrn.u16  d0, q8, #8                    \n"  // 16 bit to 8 bit U
    "vqshrn.u16  d1, q9, #8                    \n"  // 16 bit to 8 bit V
    MEMACCESS(2)
    "vst1.8     {d0}, [%2]!                    \n"  // store 8 pixels U.
    MEMACCESS(3)
    "vst1.8     {d1}, [%3]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_rgb565),  // %0
    "+r"(src_stride_rgb565),  // %1
    "+r"(dst_u),     // %2
    "+r"(dst_v),     // %3
    "+r"(pix)        // %4
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7",
    "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_RGB565TOUVROW_NEON

// 16x2 pixels -> 8x1.  pix is number of argb pixels. e.g. 16.
#ifdef HAS_ARGB1555TOUVROW_NEON
void ARGB1555ToUVRow_NEON(const uint8* src_argb1555, int src_stride_argb1555,
                        uint8* dst_u, uint8* dst_v, int pix) {
  asm volatile (
    "add        %1, %0, %1                     \n"  // src_stride + src_argb
    "vmov.s16   q10, #112 / 2                  \n"  // UB / VR 0.875 coefficient
    "vmov.s16   q11, #74 / 2                   \n"  // UG -0.5781 coefficient
    "vmov.s16   q12, #38 / 2                   \n"  // UR -0.2969 coefficient
    "vmov.s16   q13, #18 / 2                   \n"  // VB -0.1406 coefficient
    "vmov.s16   q14, #94 / 2                   \n"  // VG -0.7344 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld1.8     {q0}, [%0]!                    \n"  // load 8 ARGB1555 pixels.
    RGB555TOARGB
    "vpaddl.u8  d8, d0                         \n"  // B 8 bytes -> 4 shorts.
    "vpaddl.u8  d10, d1                        \n"  // G 8 bytes -> 4 shorts.
    "vpaddl.u8  d12, d2                        \n"  // R 8 bytes -> 4 shorts.
    MEMACCESS(0)
    "vld1.8     {q0}, [%0]!                    \n"  // next 8 ARGB1555 pixels.
    RGB555TOARGB
    "vpaddl.u8  d9, d0                         \n"  // B 8 bytes -> 4 shorts.
    "vpaddl.u8  d11, d1                        \n"  // G 8 bytes -> 4 shorts.
    "vpaddl.u8  d13, d2                        \n"  // R 8 bytes -> 4 shorts.

    MEMACCESS(1)
    "vld1.8     {q0}, [%1]!                    \n"  // load 8 ARGB1555 pixels.
    RGB555TOARGB
    "vpadal.u8  d8, d0                         \n"  // B 8 bytes -> 4 shorts.
    "vpadal.u8  d10, d1                        \n"  // G 8 bytes -> 4 shorts.
    "vpadal.u8  d12, d2                        \n"  // R 8 bytes -> 4 shorts.
    MEMACCESS(1)
    "vld1.8     {q0}, [%1]!                    \n"  // next 8 ARGB1555 pixels.
    RGB555TOARGB
    "vpadal.u8  d9, d0                         \n"  // B 8 bytes -> 4 shorts.
    "vpadal.u8  d11, d1                        \n"  // G 8 bytes -> 4 shorts.
    "vpadal.u8  d13, d2                        \n"  // R 8 bytes -> 4 shorts.

    "vrshr.u16  q4, q4, #1                     \n"  // 2x average
    "vrshr.u16  q5, q5, #1                     \n"
    "vrshr.u16  q6, q6, #1                     \n"

    "subs       %4, %4, #16                    \n"  // 16 processed per loop.
    "vmul.s16   q8, q4, q10                    \n"  // B
    "vmls.s16   q8, q5, q11                    \n"  // G
    "vmls.s16   q8, q6, q12                    \n"  // R
    "vadd.u16   q8, q8, q15                    \n"  // +128 -> unsigned
    "vmul.s16   q9, q6, q10                    \n"  // R
    "vmls.s16   q9, q5, q14                    \n"  // G
    "vmls.s16   q9, q4, q13                    \n"  // B
    "vadd.u16   q9, q9, q15                    \n"  // +128 -> unsigned
    "vqshrn.u16  d0, q8, #8                    \n"  // 16 bit to 8 bit U
    "vqshrn.u16  d1, q9, #8                    \n"  // 16 bit to 8 bit V
    MEMACCESS(2)
    "vst1.8     {d0}, [%2]!                    \n"  // store 8 pixels U.
    MEMACCESS(3)
    "vst1.8     {d1}, [%3]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_argb1555),  // %0
    "+r"(src_stride_argb1555),  // %1
    "+r"(dst_u),     // %2
    "+r"(dst_v),     // %3
    "+r"(pix)        // %4
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7",
    "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_ARGB1555TOUVROW_NEON

// 16x2 pixels -> 8x1.  pix is number of argb pixels. e.g. 16.
#ifdef HAS_ARGB4444TOUVROW_NEON
void ARGB4444ToUVRow_NEON(const uint8* src_argb4444, int src_stride_argb4444,
                          uint8* dst_u, uint8* dst_v, int pix) {
  asm volatile (
    "add        %1, %0, %1                     \n"  // src_stride + src_argb
    "vmov.s16   q10, #112 / 2                  \n"  // UB / VR 0.875 coefficient
    "vmov.s16   q11, #74 / 2                   \n"  // UG -0.5781 coefficient
    "vmov.s16   q12, #38 / 2                   \n"  // UR -0.2969 coefficient
    "vmov.s16   q13, #18 / 2                   \n"  // VB -0.1406 coefficient
    "vmov.s16   q14, #94 / 2                   \n"  // VG -0.7344 coefficient
    "vmov.u16   q15, #0x8080                   \n"  // 128.5
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld1.8     {q0}, [%0]!                    \n"  // load 8 ARGB4444 pixels.
    ARGB4444TOARGB
    "vpaddl.u8  d8, d0                         \n"  // B 8 bytes -> 4 shorts.
    "vpaddl.u8  d10, d1                        \n"  // G 8 bytes -> 4 shorts.
    "vpaddl.u8  d12, d2                        \n"  // R 8 bytes -> 4 shorts.
    MEMACCESS(0)
    "vld1.8     {q0}, [%0]!                    \n"  // next 8 ARGB4444 pixels.
    ARGB4444TOARGB
    "vpaddl.u8  d9, d0                         \n"  // B 8 bytes -> 4 shorts.
    "vpaddl.u8  d11, d1                        \n"  // G 8 bytes -> 4 shorts.
    "vpaddl.u8  d13, d2                        \n"  // R 8 bytes -> 4 shorts.

    MEMACCESS(1)
    "vld1.8     {q0}, [%1]!                    \n"  // load 8 ARGB4444 pixels.
    ARGB4444TOARGB
    "vpadal.u8  d8, d0                         \n"  // B 8 bytes -> 4 shorts.
    "vpadal.u8  d10, d1                        \n"  // G 8 bytes -> 4 shorts.
    "vpadal.u8  d12, d2                        \n"  // R 8 bytes -> 4 shorts.
    MEMACCESS(1)
    "vld1.8     {q0}, [%1]!                    \n"  // next 8 ARGB4444 pixels.
    ARGB4444TOARGB
    "vpadal.u8  d9, d0                         \n"  // B 8 bytes -> 4 shorts.
    "vpadal.u8  d11, d1                        \n"  // G 8 bytes -> 4 shorts.
    "vpadal.u8  d13, d2                        \n"  // R 8 bytes -> 4 shorts.

    "vrshr.u16  q4, q4, #1                     \n"  // 2x average
    "vrshr.u16  q5, q5, #1                     \n"
    "vrshr.u16  q6, q6, #1                     \n"

    "subs       %4, %4, #16                    \n"  // 16 processed per loop.
    "vmul.s16   q8, q4, q10                    \n"  // B
    "vmls.s16   q8, q5, q11                    \n"  // G
    "vmls.s16   q8, q6, q12                    \n"  // R
    "vadd.u16   q8, q8, q15                    \n"  // +128 -> unsigned
    "vmul.s16   q9, q6, q10                    \n"  // R
    "vmls.s16   q9, q5, q14                    \n"  // G
    "vmls.s16   q9, q4, q13                    \n"  // B
    "vadd.u16   q9, q9, q15                    \n"  // +128 -> unsigned
    "vqshrn.u16  d0, q8, #8                    \n"  // 16 bit to 8 bit U
    "vqshrn.u16  d1, q9, #8                    \n"  // 16 bit to 8 bit V
    MEMACCESS(2)
    "vst1.8     {d0}, [%2]!                    \n"  // store 8 pixels U.
    MEMACCESS(3)
    "vst1.8     {d1}, [%3]!                    \n"  // store 8 pixels V.
    "bgt        1b                             \n"
  : "+r"(src_argb4444),  // %0
    "+r"(src_stride_argb4444),  // %1
    "+r"(dst_u),     // %2
    "+r"(dst_v),     // %3
    "+r"(pix)        // %4
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7",
    "q8", "q9", "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_ARGB4444TOUVROW_NEON

#ifdef HAS_RGB565TOYROW_NEON
void RGB565ToYRow_NEON(const uint8* src_rgb565, uint8* dst_y, int pix) {
  asm volatile (
    "vmov.u8    d24, #13                       \n"  // B * 0.1016 coefficient
    "vmov.u8    d25, #65                       \n"  // G * 0.5078 coefficient
    "vmov.u8    d26, #33                       \n"  // R * 0.2578 coefficient
    "vmov.u8    d27, #16                       \n"  // Add 16 constant
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld1.8     {q0}, [%0]!                    \n"  // load 8 RGB565 pixels.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    RGB565TOARGB
    "vmull.u8   q2, d0, d24                    \n"  // B
    "vmlal.u8   q2, d1, d25                    \n"  // G
    "vmlal.u8   q2, d2, d26                    \n"  // R
    "vqrshrun.s16 d0, q2, #7                   \n"  // 16 bit to 8 bit Y
    "vqadd.u8   d0, d27                        \n"
    MEMACCESS(1)
    "vst1.8     {d0}, [%1]!                    \n"  // store 8 pixels Y.
    "bgt        1b                             \n"
  : "+r"(src_rgb565),  // %0
    "+r"(dst_y),       // %1
    "+r"(pix)          // %2
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q12", "q13"
  );
}
#endif  // HAS_RGB565TOYROW_NEON

#ifdef HAS_ARGB1555TOYROW_NEON
void ARGB1555ToYRow_NEON(const uint8* src_argb1555, uint8* dst_y, int pix) {
  asm volatile (
    "vmov.u8    d24, #13                       \n"  // B * 0.1016 coefficient
    "vmov.u8    d25, #65                       \n"  // G * 0.5078 coefficient
    "vmov.u8    d26, #33                       \n"  // R * 0.2578 coefficient
    "vmov.u8    d27, #16                       \n"  // Add 16 constant
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld1.8     {q0}, [%0]!                    \n"  // load 8 ARGB1555 pixels.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    ARGB1555TOARGB
    "vmull.u8   q2, d0, d24                    \n"  // B
    "vmlal.u8   q2, d1, d25                    \n"  // G
    "vmlal.u8   q2, d2, d26                    \n"  // R
    "vqrshrun.s16 d0, q2, #7                   \n"  // 16 bit to 8 bit Y
    "vqadd.u8   d0, d27                        \n"
    MEMACCESS(1)
    "vst1.8     {d0}, [%1]!                    \n"  // store 8 pixels Y.
    "bgt        1b                             \n"
  : "+r"(src_argb1555),  // %0
    "+r"(dst_y),         // %1
    "+r"(pix)            // %2
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q12", "q13"
  );
}
#endif  // HAS_ARGB1555TOYROW_NEON

#ifdef HAS_ARGB4444TOYROW_NEON
void ARGB4444ToYRow_NEON(const uint8* src_argb4444, uint8* dst_y, int pix) {
  asm volatile (
    "vmov.u8    d24, #13                       \n"  // B * 0.1016 coefficient
    "vmov.u8    d25, #65                       \n"  // G * 0.5078 coefficient
    "vmov.u8    d26, #33                       \n"  // R * 0.2578 coefficient
    "vmov.u8    d27, #16                       \n"  // Add 16 constant
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld1.8     {q0}, [%0]!                    \n"  // load 8 ARGB4444 pixels.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    ARGB4444TOARGB
    "vmull.u8   q2, d0, d24                    \n"  // B
    "vmlal.u8   q2, d1, d25                    \n"  // G
    "vmlal.u8   q2, d2, d26                    \n"  // R
    "vqrshrun.s16 d0, q2, #7                   \n"  // 16 bit to 8 bit Y
    "vqadd.u8   d0, d27                        \n"
    MEMACCESS(1)
    "vst1.8     {d0}, [%1]!                    \n"  // store 8 pixels Y.
    "bgt        1b                             \n"
  : "+r"(src_argb4444),  // %0
    "+r"(dst_y),         // %1
    "+r"(pix)            // %2
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q12", "q13"
  );
}
#endif  // HAS_ARGB4444TOYROW_NEON

#ifdef HAS_BGRATOYROW_NEON
void BGRAToYRow_NEON(const uint8* src_bgra, uint8* dst_y, int pix) {
  asm volatile (
    "vmov.u8    d4, #33                        \n"  // R * 0.2578 coefficient
    "vmov.u8    d5, #65                        \n"  // G * 0.5078 coefficient
    "vmov.u8    d6, #13                        \n"  // B * 0.1016 coefficient
    "vmov.u8    d7, #16                        \n"  // Add 16 constant
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d1, d2, d3}, [%0]!        \n"  // load 8 pixels of BGRA.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "vmull.u8   q8, d1, d4                     \n"  // R
    "vmlal.u8   q8, d2, d5                     \n"  // G
    "vmlal.u8   q8, d3, d6                     \n"  // B
    "vqrshrun.s16 d0, q8, #7                   \n"  // 16 bit to 8 bit Y
    "vqadd.u8   d0, d7                         \n"
    MEMACCESS(1)
    "vst1.8     {d0}, [%1]!                    \n"  // store 8 pixels Y.
    "bgt        1b                             \n"
  : "+r"(src_bgra),  // %0
    "+r"(dst_y),     // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "d0", "d1", "d2", "d3", "d4", "d5", "d6", "d7", "q8"
  );
}
#endif  // HAS_BGRATOYROW_NEON

#ifdef HAS_ABGRTOYROW_NEON
void ABGRToYRow_NEON(const uint8* src_abgr, uint8* dst_y, int pix) {
  asm volatile (
    "vmov.u8    d4, #33                        \n"  // R * 0.2578 coefficient
    "vmov.u8    d5, #65                        \n"  // G * 0.5078 coefficient
    "vmov.u8    d6, #13                        \n"  // B * 0.1016 coefficient
    "vmov.u8    d7, #16                        \n"  // Add 16 constant
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d1, d2, d3}, [%0]!        \n"  // load 8 pixels of ABGR.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "vmull.u8   q8, d0, d4                     \n"  // R
    "vmlal.u8   q8, d1, d5                     \n"  // G
    "vmlal.u8   q8, d2, d6                     \n"  // B
    "vqrshrun.s16 d0, q8, #7                   \n"  // 16 bit to 8 bit Y
    "vqadd.u8   d0, d7                         \n"
    MEMACCESS(1)
    "vst1.8     {d0}, [%1]!                    \n"  // store 8 pixels Y.
    "bgt        1b                             \n"
  : "+r"(src_abgr),  // %0
    "+r"(dst_y),  // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "d0", "d1", "d2", "d3", "d4", "d5", "d6", "d7", "q8"
  );
}
#endif  // HAS_ABGRTOYROW_NEON

#ifdef HAS_RGBATOYROW_NEON
void RGBAToYRow_NEON(const uint8* src_rgba, uint8* dst_y, int pix) {
  asm volatile (
    "vmov.u8    d4, #13                        \n"  // B * 0.1016 coefficient
    "vmov.u8    d5, #65                        \n"  // G * 0.5078 coefficient
    "vmov.u8    d6, #33                        \n"  // R * 0.2578 coefficient
    "vmov.u8    d7, #16                        \n"  // Add 16 constant
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d1, d2, d3}, [%0]!        \n"  // load 8 pixels of RGBA.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "vmull.u8   q8, d1, d4                     \n"  // B
    "vmlal.u8   q8, d2, d5                     \n"  // G
    "vmlal.u8   q8, d3, d6                     \n"  // R
    "vqrshrun.s16 d0, q8, #7                   \n"  // 16 bit to 8 bit Y
    "vqadd.u8   d0, d7                         \n"
    MEMACCESS(1)
    "vst1.8     {d0}, [%1]!                    \n"  // store 8 pixels Y.
    "bgt        1b                             \n"
  : "+r"(src_rgba),  // %0
    "+r"(dst_y),  // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "d0", "d1", "d2", "d3", "d4", "d5", "d6", "d7", "q8"
  );
}
#endif  // HAS_RGBATOYROW_NEON

#ifdef HAS_RGB24TOYROW_NEON
void RGB24ToYRow_NEON(const uint8* src_rgb24, uint8* dst_y, int pix) {
  asm volatile (
    "vmov.u8    d4, #13                        \n"  // B * 0.1016 coefficient
    "vmov.u8    d5, #65                        \n"  // G * 0.5078 coefficient
    "vmov.u8    d6, #33                        \n"  // R * 0.2578 coefficient
    "vmov.u8    d7, #16                        \n"  // Add 16 constant
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld3.8     {d0, d1, d2}, [%0]!            \n"  // load 8 pixels of RGB24.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "vmull.u8   q8, d0, d4                     \n"  // B
    "vmlal.u8   q8, d1, d5                     \n"  // G
    "vmlal.u8   q8, d2, d6                     \n"  // R
    "vqrshrun.s16 d0, q8, #7                   \n"  // 16 bit to 8 bit Y
    "vqadd.u8   d0, d7                         \n"
    MEMACCESS(1)
    "vst1.8     {d0}, [%1]!                    \n"  // store 8 pixels Y.
    "bgt        1b                             \n"
  : "+r"(src_rgb24),  // %0
    "+r"(dst_y),  // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "d0", "d1", "d2", "d3", "d4", "d5", "d6", "d7", "q8"
  );
}
#endif  // HAS_RGB24TOYROW_NEON

#ifdef HAS_RAWTOYROW_NEON
void RAWToYRow_NEON(const uint8* src_raw, uint8* dst_y, int pix) {
  asm volatile (
    "vmov.u8    d4, #33                        \n"  // R * 0.2578 coefficient
    "vmov.u8    d5, #65                        \n"  // G * 0.5078 coefficient
    "vmov.u8    d6, #13                        \n"  // B * 0.1016 coefficient
    "vmov.u8    d7, #16                        \n"  // Add 16 constant
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld3.8     {d0, d1, d2}, [%0]!            \n"  // load 8 pixels of RAW.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "vmull.u8   q8, d0, d4                     \n"  // B
    "vmlal.u8   q8, d1, d5                     \n"  // G
    "vmlal.u8   q8, d2, d6                     \n"  // R
    "vqrshrun.s16 d0, q8, #7                   \n"  // 16 bit to 8 bit Y
    "vqadd.u8   d0, d7                         \n"
    MEMACCESS(1)
    "vst1.8     {d0}, [%1]!                    \n"  // store 8 pixels Y.
    "bgt        1b                             \n"
  : "+r"(src_raw),  // %0
    "+r"(dst_y),  // %1
    "+r"(pix)        // %2
  :
  : "cc", "memory", "d0", "d1", "d2", "d3", "d4", "d5", "d6", "d7", "q8"
  );
}
#endif  // HAS_RAWTOYROW_NEON

// Bilinear filter 16x2 -> 16x1
#ifdef HAS_INTERPOLATEROW_NEON
void InterpolateRow_NEON(uint8* dst_ptr,
                         const uint8* src_ptr, ptrdiff_t src_stride,
                         int dst_width, int source_y_fraction) {
  asm volatile (
    "cmp        %4, #0                         \n"
    "beq        100f                           \n"
    "add        %2, %1                         \n"
    "cmp        %4, #64                        \n"
    "beq        75f                            \n"
    "cmp        %4, #128                       \n"
    "beq        50f                            \n"
    "cmp        %4, #192                       \n"
    "beq        25f                            \n"

    "vdup.8     d5, %4                         \n"
    "rsb        %4, #256                       \n"
    "vdup.8     d4, %4                         \n"
    // General purpose row blend.
  "1:                                          \n"
    MEMACCESS(1)
    "vld1.8     {q0}, [%1]!                    \n"
    MEMACCESS(2)
    "vld1.8     {q1}, [%2]!                    \n"
    "subs       %3, %3, #16                    \n"
    "vmull.u8   q13, d0, d4                    \n"
    "vmull.u8   q14, d1, d4                    \n"
    "vmlal.u8   q13, d2, d5                    \n"
    "vmlal.u8   q14, d3, d5                    \n"
    "vrshrn.u16 d0, q13, #8                    \n"
    "vrshrn.u16 d1, q14, #8                    \n"
    MEMACCESS(0)
    "vst1.8     {q0}, [%0]!                    \n"
    "bgt        1b                             \n"
    "b          99f                            \n"

    // Blend 25 / 75.
  "25:                                         \n"
    MEMACCESS(1)
    "vld1.8     {q0}, [%1]!                    \n"
    MEMACCESS(2)
    "vld1.8     {q1}, [%2]!                    \n"
    "subs       %3, %3, #16                    \n"
    "vrhadd.u8  q0, q1                         \n"
    "vrhadd.u8  q0, q1                         \n"
    MEMACCESS(0)
    "vst1.8     {q0}, [%0]!                    \n"
    "bgt        25b                            \n"
    "b          99f                            \n"

    // Blend 50 / 50.
  "50:                                         \n"
    MEMACCESS(1)
    "vld1.8     {q0}, [%1]!                    \n"
    MEMACCESS(2)
    "vld1.8     {q1}, [%2]!                    \n"
    "subs       %3, %3, #16                    \n"
    "vrhadd.u8  q0, q1                         \n"
    MEMACCESS(0)
    "vst1.8     {q0}, [%0]!                    \n"
    "bgt        50b                            \n"
    "b          99f                            \n"

    // Blend 75 / 25.
  "75:                                         \n"
    MEMACCESS(1)
    "vld1.8     {q1}, [%1]!                    \n"
    MEMACCESS(2)
    "vld1.8     {q0}, [%2]!                    \n"
    "subs       %3, %3, #16                    \n"
    "vrhadd.u8  q0, q1                         \n"
    "vrhadd.u8  q0, q1                         \n"
    MEMACCESS(0)
    "vst1.8     {q0}, [%0]!                    \n"
    "bgt        75b                            \n"
    "b          99f                            \n"

    // Blend 100 / 0 - Copy row unchanged.
  "100:                                        \n"
    MEMACCESS(1)
    "vld1.8     {q0}, [%1]!                    \n"
    "subs       %3, %3, #16                    \n"
    MEMACCESS(0)
    "vst1.8     {q0}, [%0]!                    \n"
    "bgt        100b                           \n"

  "99:                                         \n"
  : "+r"(dst_ptr),          // %0
    "+r"(src_ptr),          // %1
    "+r"(src_stride),       // %2
    "+r"(dst_width),        // %3
    "+r"(source_y_fraction) // %4
  :
  : "cc", "memory", "q0", "q1", "d4", "d5", "q13", "q14"
  );
}
#endif  // HAS_INTERPOLATEROW_NEON

// dr * (256 - sa) / 256 + sr = dr - dr * sa / 256 + sr
#ifdef HAS_ARGBBLENDROW_NEON
void ARGBBlendRow_NEON(const uint8* src_argb0, const uint8* src_argb1,
                       uint8* dst_argb, int width) {
  asm volatile (
    "subs       %3, #8                         \n"
    "blt        89f                            \n"
    // Blend 8 pixels.
  "8:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d1, d2, d3}, [%0]!        \n"  // load 8 pixels of ARGB0.
    MEMACCESS(1)
    "vld4.8     {d4, d5, d6, d7}, [%1]!        \n"  // load 8 pixels of ARGB1.
    "subs       %3, %3, #8                     \n"  // 8 processed per loop.
    "vmull.u8   q10, d4, d3                    \n"  // db * a
    "vmull.u8   q11, d5, d3                    \n"  // dg * a
    "vmull.u8   q12, d6, d3                    \n"  // dr * a
    "vqrshrn.u16 d20, q10, #8                  \n"  // db >>= 8
    "vqrshrn.u16 d21, q11, #8                  \n"  // dg >>= 8
    "vqrshrn.u16 d22, q12, #8                  \n"  // dr >>= 8
    "vqsub.u8   q2, q2, q10                    \n"  // dbg - dbg * a / 256
    "vqsub.u8   d6, d6, d22                    \n"  // dr - dr * a / 256
    "vqadd.u8   q0, q0, q2                     \n"  // + sbg
    "vqadd.u8   d2, d2, d6                     \n"  // + sr
    "vmov.u8    d3, #255                       \n"  // a = 255
    MEMACCESS(2)
    "vst4.8     {d0, d1, d2, d3}, [%2]!        \n"  // store 8 pixels of ARGB.
    "bge        8b                             \n"

  "89:                                         \n"
    "adds       %3, #8-1                       \n"
    "blt        99f                            \n"

    // Blend 1 pixels.
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0[0],d1[0],d2[0],d3[0]}, [%0]! \n"  // load 1 pixel ARGB0.
    MEMACCESS(1)
    "vld4.8     {d4[0],d5[0],d6[0],d7[0]}, [%1]! \n"  // load 1 pixel ARGB1.
    "subs       %3, %3, #1                     \n"  // 1 processed per loop.
    "vmull.u8   q10, d4, d3                    \n"  // db * a
    "vmull.u8   q11, d5, d3                    \n"  // dg * a
    "vmull.u8   q12, d6, d3                    \n"  // dr * a
    "vqrshrn.u16 d20, q10, #8                  \n"  // db >>= 8
    "vqrshrn.u16 d21, q11, #8                  \n"  // dg >>= 8
    "vqrshrn.u16 d22, q12, #8                  \n"  // dr >>= 8
    "vqsub.u8   q2, q2, q10                    \n"  // dbg - dbg * a / 256
    "vqsub.u8   d6, d6, d22                    \n"  // dr - dr * a / 256
    "vqadd.u8   q0, q0, q2                     \n"  // + sbg
    "vqadd.u8   d2, d2, d6                     \n"  // + sr
    "vmov.u8    d3, #255                       \n"  // a = 255
    MEMACCESS(2)
    "vst4.8     {d0[0],d1[0],d2[0],d3[0]}, [%2]! \n"  // store 1 pixel.
    "bge        1b                             \n"

  "99:                                         \n"

  : "+r"(src_argb0),    // %0
    "+r"(src_argb1),    // %1
    "+r"(dst_argb),     // %2
    "+r"(width)         // %3
  :
  : "cc", "memory", "q0", "q1", "q2", "q3", "q10", "q11", "q12"
  );
}
#endif  // HAS_ARGBBLENDROW_NEON

// Attenuate 8 pixels at a time.
#ifdef HAS_ARGBATTENUATEROW_NEON
void ARGBAttenuateRow_NEON(const uint8* src_argb, uint8* dst_argb, int width) {
  asm volatile (
    // Attenuate 8 pixels.
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d1, d2, d3}, [%0]!        \n"  // load 8 pixels of ARGB.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "vmull.u8   q10, d0, d3                    \n"  // b * a
    "vmull.u8   q11, d1, d3                    \n"  // g * a
    "vmull.u8   q12, d2, d3                    \n"  // r * a
    "vqrshrn.u16 d0, q10, #8                   \n"  // b >>= 8
    "vqrshrn.u16 d1, q11, #8                   \n"  // g >>= 8
    "vqrshrn.u16 d2, q12, #8                   \n"  // r >>= 8
    MEMACCESS(1)
    "vst4.8     {d0, d1, d2, d3}, [%1]!        \n"  // store 8 pixels of ARGB.
    "bgt        1b                             \n"
  : "+r"(src_argb),   // %0
    "+r"(dst_argb),   // %1
    "+r"(width)       // %2
  :
  : "cc", "memory", "q0", "q1", "q10", "q11", "q12"
  );
}
#endif  // HAS_ARGBATTENUATEROW_NEON

// Quantize 8 ARGB pixels (32 bytes).
// dst = (dst * scale >> 16) * interval_size + interval_offset;
#ifdef HAS_ARGBQUANTIZEROW_NEON
void ARGBQuantizeRow_NEON(uint8* dst_argb, int scale, int interval_size,
                          int interval_offset, int width) {
  asm volatile (
    "vdup.u16   q8, %2                         \n"
    "vshr.u16   q8, q8, #1                     \n"  // scale >>= 1
    "vdup.u16   q9, %3                         \n"  // interval multiply.
    "vdup.u16   q10, %4                        \n"  // interval add

    // 8 pixel loop.
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d2, d4, d6}, [%0]         \n"  // load 8 pixels of ARGB.
    "subs       %1, %1, #8                     \n"  // 8 processed per loop.
    "vmovl.u8   q0, d0                         \n"  // b (0 .. 255)
    "vmovl.u8   q1, d2                         \n"
    "vmovl.u8   q2, d4                         \n"
    "vqdmulh.s16 q0, q0, q8                    \n"  // b * scale
    "vqdmulh.s16 q1, q1, q8                    \n"  // g
    "vqdmulh.s16 q2, q2, q8                    \n"  // r
    "vmul.u16   q0, q0, q9                     \n"  // b * interval_size
    "vmul.u16   q1, q1, q9                     \n"  // g
    "vmul.u16   q2, q2, q9                     \n"  // r
    "vadd.u16   q0, q0, q10                    \n"  // b + interval_offset
    "vadd.u16   q1, q1, q10                    \n"  // g
    "vadd.u16   q2, q2, q10                    \n"  // r
    "vqmovn.u16 d0, q0                         \n"
    "vqmovn.u16 d2, q1                         \n"
    "vqmovn.u16 d4, q2                         \n"
    MEMACCESS(0)
    "vst4.8     {d0, d2, d4, d6}, [%0]!        \n"  // store 8 pixels of ARGB.
    "bgt        1b                             \n"
  : "+r"(dst_argb),       // %0
    "+r"(width)           // %1
  : "r"(scale),           // %2
    "r"(interval_size),   // %3
    "r"(interval_offset)  // %4
  : "cc", "memory", "q0", "q1", "q2", "q3", "q8", "q9", "q10"
  );
}
#endif  // HAS_ARGBQUANTIZEROW_NEON

// Shade 8 pixels at a time by specified value.
// NOTE vqrdmulh.s16 q10, q10, d0[0] must use a scaler register from 0 to 8.
// Rounding in vqrdmulh does +1 to high if high bit of low s16 is set.
#ifdef HAS_ARGBSHADEROW_NEON
void ARGBShadeRow_NEON(const uint8* src_argb, uint8* dst_argb, int width,
                       uint32 value) {
  asm volatile (
    "vdup.u32   q0, %3                         \n"  // duplicate scale value.
    "vzip.u8    d0, d1                         \n"  // d0 aarrggbb.
    "vshr.u16   q0, q0, #1                     \n"  // scale / 2.

    // 8 pixel loop.
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d20, d22, d24, d26}, [%0]!    \n"  // load 8 pixels of ARGB.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "vmovl.u8   q10, d20                       \n"  // b (0 .. 255)
    "vmovl.u8   q11, d22                       \n"
    "vmovl.u8   q12, d24                       \n"
    "vmovl.u8   q13, d26                       \n"
    "vqrdmulh.s16 q10, q10, d0[0]              \n"  // b * scale * 2
    "vqrdmulh.s16 q11, q11, d0[1]              \n"  // g
    "vqrdmulh.s16 q12, q12, d0[2]              \n"  // r
    "vqrdmulh.s16 q13, q13, d0[3]              \n"  // a
    "vqmovn.u16 d20, q10                       \n"
    "vqmovn.u16 d22, q11                       \n"
    "vqmovn.u16 d24, q12                       \n"
    "vqmovn.u16 d26, q13                       \n"
    MEMACCESS(1)
    "vst4.8     {d20, d22, d24, d26}, [%1]!    \n"  // store 8 pixels of ARGB.
    "bgt        1b                             \n"
  : "+r"(src_argb),       // %0
    "+r"(dst_argb),       // %1
    "+r"(width)           // %2
  : "r"(value)            // %3
  : "cc", "memory", "q0", "q10", "q11", "q12", "q13"
  );
}
#endif  // HAS_ARGBSHADEROW_NEON

// Convert 8 ARGB pixels (64 bytes) to 8 Gray ARGB pixels
// Similar to ARGBToYJ but stores ARGB.
// C code is (15 * b + 75 * g + 38 * r + 64) >> 7;
#ifdef HAS_ARGBGRAYROW_NEON
void ARGBGrayRow_NEON(const uint8* src_argb, uint8* dst_argb, int width) {
  asm volatile (
    "vmov.u8    d24, #15                       \n"  // B * 0.11400 coefficient
    "vmov.u8    d25, #75                       \n"  // G * 0.58700 coefficient
    "vmov.u8    d26, #38                       \n"  // R * 0.29900 coefficient
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d1, d2, d3}, [%0]!        \n"  // load 8 ARGB pixels.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "vmull.u8   q2, d0, d24                    \n"  // B
    "vmlal.u8   q2, d1, d25                    \n"  // G
    "vmlal.u8   q2, d2, d26                    \n"  // R
    "vqrshrun.s16 d0, q2, #7                   \n"  // 15 bit to 8 bit B
    "vmov       d1, d0                         \n"  // G
    "vmov       d2, d0                         \n"  // R
    MEMACCESS(1)
    "vst4.8     {d0, d1, d2, d3}, [%1]!        \n"  // store 8 ARGB pixels.
    "bgt        1b                             \n"
  : "+r"(src_argb),  // %0
    "+r"(dst_argb),  // %1
    "+r"(width)      // %2
  :
  : "cc", "memory", "q0", "q1", "q2", "q12", "q13"
  );
}
#endif  // HAS_ARGBGRAYROW_NEON

// Convert 8 ARGB pixels (32 bytes) to 8 Sepia ARGB pixels.
//    b = (r * 35 + g * 68 + b * 17) >> 7
//    g = (r * 45 + g * 88 + b * 22) >> 7
//    r = (r * 50 + g * 98 + b * 24) >> 7

#ifdef HAS_ARGBSEPIAROW_NEON
void ARGBSepiaRow_NEON(uint8* dst_argb, int width) {
  asm volatile (
    "vmov.u8    d20, #17                       \n"  // BB coefficient
    "vmov.u8    d21, #68                       \n"  // BG coefficient
    "vmov.u8    d22, #35                       \n"  // BR coefficient
    "vmov.u8    d24, #22                       \n"  // GB coefficient
    "vmov.u8    d25, #88                       \n"  // GG coefficient
    "vmov.u8    d26, #45                       \n"  // GR coefficient
    "vmov.u8    d28, #24                       \n"  // BB coefficient
    "vmov.u8    d29, #98                       \n"  // BG coefficient
    "vmov.u8    d30, #50                       \n"  // BR coefficient
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d0, d1, d2, d3}, [%0]         \n"  // load 8 ARGB pixels.
    "subs       %1, %1, #8                     \n"  // 8 processed per loop.
    "vmull.u8   q2, d0, d20                    \n"  // B to Sepia B
    "vmlal.u8   q2, d1, d21                    \n"  // G
    "vmlal.u8   q2, d2, d22                    \n"  // R
    "vmull.u8   q3, d0, d24                    \n"  // B to Sepia G
    "vmlal.u8   q3, d1, d25                    \n"  // G
    "vmlal.u8   q3, d2, d26                    \n"  // R
    "vmull.u8   q8, d0, d28                    \n"  // B to Sepia R
    "vmlal.u8   q8, d1, d29                    \n"  // G
    "vmlal.u8   q8, d2, d30                    \n"  // R
    "vqshrn.u16 d0, q2, #7                     \n"  // 16 bit to 8 bit B
    "vqshrn.u16 d1, q3, #7                     \n"  // 16 bit to 8 bit G
    "vqshrn.u16 d2, q8, #7                     \n"  // 16 bit to 8 bit R
    MEMACCESS(0)
    "vst4.8     {d0, d1, d2, d3}, [%0]!        \n"  // store 8 ARGB pixels.
    "bgt        1b                             \n"
  : "+r"(dst_argb),  // %0
    "+r"(width)      // %1
  :
  : "cc", "memory", "q0", "q1", "q2", "q3",
    "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_ARGBSEPIAROW_NEON

// Tranform 8 ARGB pixels (32 bytes) with color matrix.
// TODO(fbarchard): Was same as Sepia except matrix is provided.  This function
// needs to saturate.  Consider doing a non-saturating version.
#ifdef HAS_ARGBCOLORMATRIXROW_NEON
void ARGBColorMatrixRow_NEON(const uint8* src_argb, uint8* dst_argb,
                             const int8* matrix_argb, int width) {
  asm volatile (
    MEMACCESS(3)
    "vld1.8     {q2}, [%3]                     \n"  // load 3 ARGB vectors.
    "vmovl.s8   q0, d4                         \n"  // B,G coefficients s16.
    "vmovl.s8   q1, d5                         \n"  // R,A coefficients s16.

    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "vld4.8     {d16, d18, d20, d22}, [%0]!    \n"  // load 8 ARGB pixels.
    "subs       %2, %2, #8                     \n"  // 8 processed per loop.
    "vmovl.u8   q8, d16                        \n"  // b (0 .. 255) 16 bit
    "vmovl.u8   q9, d18                        \n"  // g
    "vmovl.u8   q10, d20                       \n"  // r
    "vmovl.u8   q15, d22                       \n"  // a
    "vmul.s16   q12, q8, d0[0]                 \n"  // B = B * Matrix B
    "vmul.s16   q13, q8, d1[0]                 \n"  // G = B * Matrix G
    "vmul.s16   q14, q8, d2[0]                 \n"  // R = B * Matrix R
    "vmul.s16   q15, q8, d3[0]                 \n"  // A = B * Matrix A
    "vmul.s16   q4, q9, d0[1]                  \n"  // B += G * Matrix B
    "vmul.s16   q5, q9, d1[1]                  \n"  // G += G * Matrix G
    "vmul.s16   q6, q9, d2[1]                  \n"  // R += G * Matrix R
    "vmul.s16   q7, q9, d3[1]                  \n"  // A += G * Matrix A
    "vqadd.s16  q12, q12, q4                   \n"  // Accumulate B
    "vqadd.s16  q13, q13, q5                   \n"  // Accumulate G
    "vqadd.s16  q14, q14, q6                   \n"  // Accumulate R
    "vqadd.s16  q15, q15, q7                   \n"  // Accumulate A
    "vmul.s16   q4, q10, d0[2]                 \n"  // B += R * Matrix B
    "vmul.s16   q5, q10, d1[2]                 \n"  // G += R * Matrix G
    "vmul.s16   q6, q10, d2[2]                 \n"  // R += R * Matrix R
    "vmul.s16   q7, q10, d3[2]                 \n"  // A += R * Matrix A
    "vqadd.s16  q12, q12, q4                   \n"  // Accumulate B
    "vqadd.s16  q13, q13, q5                   \n"  // Accumulate G
    "vqadd.s16  q14, q14, q6                   \n"  // Accumulate R
    "vqadd.s16  q15, q15, q7                   \n"  // Accumulate A
    "vmul.s16   q4, q15, d0[3]                 \n"  // B += A * Matrix B
    "vmul.s16   q5, q15, d1[3]                 \n"  // G += A * Matrix G
    "vmul.s16   q6, q15, d2[3]                 \n"  // R += A * Matrix R
    "vmul.s16   q7, q15, d3[3]                 \n"  // A += A * Matrix A
    "vqadd.s16  q12, q12, q4                   \n"  // Accumulate B
    "vqadd.s16  q13, q13, q5                   \n"  // Accumulate G
    "vqadd.s16  q14, q14, q6                   \n"  // Accumulate R
    "vqadd.s16  q15, q15, q7                   \n"  // Accumulate A
    "vqshrun.s16 d16, q12, #6                  \n"  // 16 bit to 8 bit B
    "vqshrun.s16 d18, q13, #6                  \n"  // 16 bit to 8 bit G
    "vqshrun.s16 d20, q14, #6                  \n"  // 16 bit to 8 bit R
    "vqshrun.s16 d22, q15, #6                  \n"  // 16 bit to 8 bit A
    MEMACCESS(1)
    "vst4.8     {d16, d18, d20, d22}, [%1]!    \n"  // store 8 ARGB pixels.
    "bgt        1b                             \n"
  : "+r"(src_argb),   // %0
    "+r"(dst_argb),   // %1
    "+r"(width)       // %2
  : "r"(matrix_argb)  // %3
  : "cc", "memory", "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7", "q8", "q9",
    "q10", "q11", "q12", "q13", "q14", "q15"
  );
}
#endif  // HAS_ARGBCOLORMATRIXROW_NEON

// TODO(fbarchard): fix vqshrun in ARGBMultiplyRow_NEON and reenable.
// Multiply 2 rows of ARGB pixels together, 8 pixels at a time.
#ifdef HAS_ARGBMULTIPLYROW_NEON
void ARGBMultiplyRow_NEON(const uint8* src_argb0, const uint8* src_argb1,
                          uint8* dst_argb, int width) {
  asm volatile (
    // 8 pixel loop.
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld4        {v0.8b-v3.8b}, [%0], #32       \n"  // load 8 ARGB pixels.
    MEMACCESS(1)
    "ld4        {v4.8b-v7.8b}, [%1], #32       \n"  // load 8 more ARGB pixels.
    "subs       %3, %3, #8                     \n"  // 8 processed per loop.
    "umull      v0.8h, v0.8b, v4.8b            \n"  // multiply B
    "umull      v1.8h, v1.8b, v5.8b            \n"  // multiply G
    "umull      v2.8h, v2.8b, v6.8b            \n"  // multiply R
    "umull      v3.8h, v3.8b, v7.8b            \n"  // multiply A
    "rshrn      v0.8b, v0.8h, #8               \n"  // 16 bit to 8 bit B
    "rshrn      v1.8b, v1.8h, #8               \n"  // 16 bit to 8 bit G
    "rshrn      v2.8b, v2.8h, #8               \n"  // 16 bit to 8 bit R
    "rshrn      v3.8b, v3.8h, #8               \n"  // 16 bit to 8 bit A
    MEMACCESS(2)
    "st4        {v0.8b-v3.8b}, [%2], #32       \n"  // store 8 ARGB pixels.
    "bgt        1b                             \n"

  : "+r"(src_argb0),  // %0
    "+r"(src_argb1),  // %1
    "+r"(dst_argb),   // %2
    "+r"(width)       // %3
  :
  : "cc", "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7"
  );
}
#endif  // HAS_ARGBMULTIPLYROW_NEON

// Add 2 rows of ARGB pixels together, 8 pixels at a time.
#ifdef HAS_ARGBADDROW_NEON
void ARGBAddRow_NEON(const uint8* src_argb0, const uint8* src_argb1,
                     uint8* dst_argb, int width) {
  asm volatile (
    // 8 pixel loop.
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld4        {v0.8b-v3.8b}, [%0], #32       \n"  // load 8 ARGB pixels.
    MEMACCESS(1)
    "ld4        {v4.8b-v7.8b}, [%1], #32       \n"  // load 8 more ARGB pixels.
    "subs       %3, %3, #8                     \n"  // 8 processed per loop.
    "uqadd      v0.8b, v0.8b, v4.8b            \n"
    "uqadd      v1.8b, v1.8b, v5.8b            \n"
    "uqadd      v2.8b, v2.8b, v6.8b            \n"
    "uqadd      v3.8b, v3.8b, v7.8b            \n"
    MEMACCESS(2)
    "st4        {v0.8b-v3.8b}, [%2], #32       \n"  // store 8 ARGB pixels.
    "bgt        1b                             \n"

  : "+r"(src_argb0),  // %0
    "+r"(src_argb1),  // %1
    "+r"(dst_argb),   // %2
    "+r"(width)       // %3
  :
  : "cc", "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7"
  );
}
#endif  // HAS_ARGBADDROW_NEON

// Subtract 2 rows of ARGB pixels, 8 pixels at a time.
#ifdef HAS_ARGBSUBTRACTROW_NEON
void ARGBSubtractRow_NEON(const uint8* src_argb0, const uint8* src_argb1,
                          uint8* dst_argb, int width) {
  asm volatile (
    // 8 pixel loop.
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld4        {v0.8b-v3.8b}, [%0], #32       \n"  // load 8 ARGB pixels.
    MEMACCESS(1)
    "ld4        {v4.8b-v7.8b}, [%1], #32       \n"  // load 8 more ARGB pixels.
    "subs       %3, %3, #8                     \n"  // 8 processed per loop.
    "uqsub      v0.8b, v0.8b, v4.8b            \n"
    "uqsub      v1.8b, v1.8b, v5.8b            \n"
    "uqsub      v2.8b, v2.8b, v6.8b            \n"
    "uqsub      v3.8b, v3.8b, v7.8b            \n"
    MEMACCESS(2)
    "st4        {v0.8b-v3.8b}, [%2], #32       \n"  // store 8 ARGB pixels.
    "bgt        1b                             \n"

  : "+r"(src_argb0),  // %0
    "+r"(src_argb1),  // %1
    "+r"(dst_argb),   // %2
    "+r"(width)       // %3
  :
  : "cc", "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7"
  );
}
#endif  // HAS_ARGBSUBTRACTROW_NEON

// Adds Sobel X and Sobel Y and stores Sobel into ARGB.
// A = 255
// R = Sobel
// G = Sobel
// B = Sobel
#ifdef HAS_SOBELROW_NEON
void SobelRow_NEON(const uint8* src_sobelx, const uint8* src_sobely,
                     uint8* dst_argb, int width) {
  asm volatile (
    "movi       v3.8b, #255                    \n"  // alpha
    // 8 pixel loop.
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld1        {v0.8b}, [%0], #8              \n"  // load 8 sobelx.
    MEMACCESS(1)
    "ld1        {v1.8b}, [%1], #8              \n"  // load 8 sobely.
    "subs       %3, %3, #8                     \n"  // 8 processed per loop.
    "uqadd      v0.8b, v0.8b, v1.8b            \n"  // add
    "mov        v1.8b, v0.8b                   \n"
    "mov        v2.8b, v0.8b                   \n"
    MEMACCESS(2)
    "st4        {v0.8b-v3.8b}, [%2], #32       \n"  // store 8 ARGB pixels.
    "bgt        1b                             \n"
  : "+r"(src_sobelx),  // %0
    "+r"(src_sobely),  // %1
    "+r"(dst_argb),    // %2
    "+r"(width)        // %3
  :
  : "cc", "memory", "v0", "v1", "v2", "v3"
  );
}
#endif  // HAS_SOBELROW_NEON

// Adds Sobel X and Sobel Y and stores Sobel into plane.
#ifdef HAS_SOBELTOPLANEROW_NEON
void SobelToPlaneRow_NEON(const uint8* src_sobelx, const uint8* src_sobely,
                          uint8* dst_y, int width) {
  asm volatile (
    // 16 pixel loop.
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld1        {v0.16b}, [%0], #16            \n"  // load 16 sobelx.
    MEMACCESS(1)
    "ld1        {v1.16b}, [%1], #16            \n"  // load 16 sobely.
    "subs       %3, %3, #16                    \n"  // 16 processed per loop.
    "uqadd      v0.16b, v0.16b, v1.16b         \n"  // add
    MEMACCESS(2)
    "st1        {v0.16b}, [%2], #16            \n"  // store 16 pixels.
    "bgt        1b                             \n"
  : "+r"(src_sobelx),  // %0
    "+r"(src_sobely),  // %1
    "+r"(dst_y),       // %2
    "+r"(width)        // %3
  :
  : "cc", "memory", "v0", "v1"
  );
}
#endif  // HAS_SOBELTOPLANEROW_NEON

// Mixes Sobel X, Sobel Y and Sobel into ARGB.
// A = 255
// R = Sobel X
// G = Sobel
// B = Sobel Y
#ifdef HAS_SOBELXYROW_NEON
void SobelXYRow_NEON(const uint8* src_sobelx, const uint8* src_sobely,
                     uint8* dst_argb, int width) {
  asm volatile (
    "movi       v3.8b, #255                    \n"  // alpha
    // 8 pixel loop.
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld1        {v2.8b}, [%0], #8              \n"  // load 8 sobelx.
    MEMACCESS(1)
    "ld1        {v0.8b}, [%1], #8              \n"  // load 8 sobely.
    "subs       %3, %3, #8                     \n"  // 8 processed per loop.
    "uqadd      v1.8b, v0.8b, v2.8b            \n"  // add
    MEMACCESS(2)
    "st4        {v0.8b-v3.8b}, [%2], #32       \n"  // store 8 ARGB pixels.
    "bgt        1b                             \n"
  : "+r"(src_sobelx),  // %0
    "+r"(src_sobely),  // %1
    "+r"(dst_argb),    // %2
    "+r"(width)        // %3
  :
  : "cc", "memory", "v0", "v1", "v2", "v3"
  );
}
#endif  // HAS_SOBELXYROW_NEON

// SobelX as a matrix is
// -1  0  1
// -2  0  2
// -1  0  1
#ifdef HAS_SOBELXROW_NEON
void SobelXRow_NEON(const uint8* src_y0, const uint8* src_y1,
                    const uint8* src_y2, uint8* dst_sobelx, int width) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld1        {v0.8b}, [%0],%5               \n"  // top
    MEMACCESS(0)
    "ld1        {v1.8b}, [%0],%6               \n"
    "usubl      v0.8h, v0.8b, v1.8b            \n"
    MEMACCESS(1)
    "ld1        {v2.8b}, [%1],%5               \n"  // center * 2
    MEMACCESS(1)
    "ld1        {v3.8b}, [%1],%6               \n"
    "usubl      v1.8h, v2.8b, v3.8b            \n"
    "add        v0.8h, v0.8h, v1.8h            \n"
    "add        v0.8h, v0.8h, v1.8h            \n"
    MEMACCESS(2)
    "ld1        {v2.8b}, [%2],%5               \n"  // bottom
    MEMACCESS(2)
    "ld1        {v3.8b}, [%2],%6               \n"
    "subs       %4, %4, #8                     \n"  // 8 pixels
    "usubl      v1.8h, v2.8b, v3.8b            \n"
    "add        v0.8h, v0.8h, v1.8h            \n"
    "abs        v0.8h, v0.8h                   \n"
    "uqxtn      v0.8b, v0.8h                   \n"
    MEMACCESS(3)
    "st1        {v0.8b}, [%3], #8              \n"  // store 8 sobelx
    "bgt        1b                             \n"
  : "+r"(src_y0),      // %0
    "+r"(src_y1),      // %1
    "+r"(src_y2),      // %2
    "+r"(dst_sobelx),  // %3
    "+r"(width)        // %4
  : "r"(2),            // %5
    "r"(6)             // %6
  : "cc", "memory", "v0", "v1", "v2", "v3"  // Clobber List
  );
}
#endif  // HAS_SOBELXROW_NEON

// SobelY as a matrix is
// -1 -2 -1
//  0  0  0
//  1  2  1
#ifdef HAS_SOBELYROW_NEON
void SobelYRow_NEON(const uint8* src_y0, const uint8* src_y1,
                    uint8* dst_sobely, int width) {
  asm volatile (
    ".p2align   2                              \n"
  "1:                                          \n"
    MEMACCESS(0)
    "ld1        {v0.8b}, [%0],%4               \n"  // left
    MEMACCESS(1)
    "ld1        {v1.8b}, [%1],%4               \n"
    "usubl      v0.8h, v0.8b, v1.8b            \n"
    MEMACCESS(0)
    "ld1        {v2.8b}, [%0],%4               \n"  // center * 2
    MEMACCESS(1)
    "ld1        {v3.8b}, [%1],%4               \n"
    "usubl      v1.8h, v2.8b, v3.8b            \n"
    "add        v0.8h, v0.8h, v1.8h            \n"
    "add        v0.8h, v0.8h, v1.8h            \n"
    MEMACCESS(0)
    "ld1        {v2.8b}, [%0],%5               \n"  // right
    MEMACCESS(1)
    "ld1        {v3.8b}, [%1],%5               \n"
    "subs       %3, %3, #8                     \n"  // 8 pixels
    "usubl      v1.8h, v2.8b, v3.8b            \n"
    "add        v0.8h, v0.8h, v1.8h            \n"
    "abs        v0.8h, v0.8h                   \n"
    "uqxtn      v0.8b, v0.8h                   \n"
    MEMACCESS(2)
    "st1        {v0.8b}, [%2], #8              \n"  // store 8 sobely
    "bgt        1b                             \n"
  : "+r"(src_y0),      // %0
    "+r"(src_y1),      // %1
    "+r"(dst_sobely),  // %2
    "+r"(width)        // %3
  : "r"(1),            // %4
    "r"(6)             // %5
  : "cc", "memory", "v0", "v1", "v2", "v3"  // Clobber List
  );
}
#endif  // HAS_SOBELYROW_NEON
#endif  // !defined(LIBYUV_DISABLE_NEON) && defined(__aarch64__)

#ifdef __cplusplus
}  // extern "C"
}  // namespace libyuv
#endif
