/*
 * memset / memcpy for the CRT-free build: the compiler may emit calls to these even without the C runtime
 * (e.g. for structure initialization). The loops must not be turned back into memset/memcpy calls.
 * MSVC: this file must be compiled WITHOUT whole program optimization (/GL), see the .vcxproj files.
 */
#include "common.h"

#if defined(_MSC_VER)
#include <intrin.h>
#pragma function(memset)
#pragma function(memcpy)

void* __cdecl memset(void* pDestination, int value, size_t cbCount)
{
    __stosb((unsigned char*)pDestination, (unsigned char)value, cbCount);
    return pDestination;
}

void* __cdecl memcpy(void* pDestination, const void* pSource, size_t cbCount)
{
    __movsb((unsigned char*)pDestination, (const unsigned char*)pSource, cbCount);
    return pDestination;
}
#else
__attribute__((optimize("no-tree-loop-distribute-patterns")))
void* memset(void* pDestination, int value, size_t cbCount)
{
    volatile unsigned char* p = (volatile unsigned char*)pDestination;
    while (cbCount--)
    {
        *p++ = (unsigned char)value;
    }
    return pDestination;
}

__attribute__((optimize("no-tree-loop-distribute-patterns")))
void* memcpy(void* pDestination, const void* pSource, size_t cbCount)
{
    volatile unsigned char* d = (volatile unsigned char*)pDestination;
    const unsigned char* s = (const unsigned char*)pSource;
    while (cbCount--)
    {
        *d++ = *s++;
    }
    return pDestination;
}
#endif

