// Modified original file to remove unused methods.
// Original copyright message below.

// <copyright file="Bits.cs" company="Sedat Kapanoglu">
// Copyright (c) 2015-2022 Sedat Kapanoglu
// MIT License (see LICENSE file for details)
// </copyright>

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HashDepot;

/// <summary>
/// Bit operations.
/// </summary>
internal static class Bits
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong RotateLeft(ulong value, int bits)
    {
        return (value << bits) | (value >> (64 - bits));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ToUInt64(ReadOnlySpan<byte> bytes)
    {
        return Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(bytes));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ToUInt32(ReadOnlySpan<byte> bytes)
    {
        return Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(bytes));
    }
}