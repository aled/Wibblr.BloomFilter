# Wibblr.BloomFilter

An implementation of a BloomFilter, a data structure which has 2 methods, Add() and MayContain().

If MayContain() returns false, the item has *definitely* not been added; if it returns true, the item has *probably* been added. The false positive ratio is specified
by the user at runtime (for example, 0.01, meaning 1 in 100 times that an item does not exist, MayContain() will return true).

A bloom filter is smaller than the items contained in it. For example, in 16KB of memory, over 26000 items *of any size* can be stored with a false positive ratio of 0.01.

Bloom filters are useful as an additional check before running some expensive query; when the bloom filter does not contain an item, the expensive query does not need to be run.
When the bloom filter *may* contain the item, the expensive query does need to be run, but this only happens occasionally for missing items.

This implementation is optimised for speed and low memory usage, and uses the XxHash64 hash algorithm from https://github.com/ssg/HashDepot.
