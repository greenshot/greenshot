#pragma once

#include "common.h"

/*
 * Minimal runtime replacing the few C runtime functions the proxy needs.
 * Memory comes from the process heap; strings are handled with Win32 functions.
 */

void* RtAlloc(SIZE_T cbSize);                 /* Zero-initialized, NULL on failure */
void* RtReAlloc(void* pMemory, SIZE_T cbSize); /* NULL on failure (original block stays valid) */
void RtFree(void* pMemory);                    /* Accepts NULL */

SIZE_T RtStrLenW(LPCWSTR pwszText);
BOOL RtEqualsIgnoreCaseW(LPCWSTR pwszA, LPCWSTR pwszB);
BOOL RtStartsWithW(LPCWSTR pwszText, LPCWSTR pwszPrefix, BOOL bIgnoreCase);
BOOL RtEndsWithIgnoreCaseW(LPCWSTR pwszText, LPCWSTR pwszSuffix);
BOOL RtContainsCharW(LPCWSTR pwszText, WCHAR ch);

/* Concatenates two strings into a new heap allocated string; free with RtFree */
LPWSTR RtConcatW(LPCWSTR pwszFirst, LPCWSTR pwszSecond);

/* Writes UTF-8 text to a standard handle: raw bytes for pipes/files, UTF-16 via WriteConsoleW for a console */
void RtWriteUtf8(DWORD nStdHandle, const char* pText, SIZE_T cbText);
void RtWriteString(DWORD nStdHandle, const char* pszText); /* NUL terminated UTF-8 */

/*
 * Growable JSON text buffer. Only what the proxy needs: raw fragments and correctly escaped strings.
 * Any allocation failure sets bFailed; the buffer content is then undefined.
 */
typedef struct _JSON_BUILDER
{
    char* pData;
    SIZE_T cbLength;
    SIZE_T cbCapacity;
    BOOL bFailed;
} JSON_BUILDER;

void JsonAppendRaw(JSON_BUILDER* pBuilder, const char* pszRaw);
void JsonAppendStringUtf8(JSON_BUILDER* pBuilder, const char* pszUtf8);   /* Adds "..." with escaping */
void JsonAppendStringW(JSON_BUILDER* pBuilder, LPCWSTR pwszText);         /* Converts to UTF-8, then as above */
void JsonFree(JSON_BUILDER* pBuilder);
