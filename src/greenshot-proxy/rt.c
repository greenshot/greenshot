#include "rt.h"

/* Max UTF-16 code units per WriteConsoleW call */
#define CONSOLE_WRITE_CHUNK 8192

/* ------------------------------------------------------------------------------------------------
 * Memory
 * ------------------------------------------------------------------------------------------------ */

void* RtAlloc(SIZE_T cbSize)
{
    return HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, cbSize ? cbSize : 1);
}

void* RtReAlloc(void* pMemory, SIZE_T cbSize)
{
    if (!pMemory)
    {
        return RtAlloc(cbSize);
    }
    return HeapReAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, pMemory, cbSize ? cbSize : 1);
}

void RtFree(void* pMemory)
{
    if (pMemory)
    {
        HeapFree(GetProcessHeap(), 0, pMemory);
    }
}

/* ------------------------------------------------------------------------------------------------
 * Strings
 * ------------------------------------------------------------------------------------------------ */

SIZE_T RtStrLenW(LPCWSTR pwszText)
{
    SIZE_T cch = 0;
    if (pwszText)
    {
        while (pwszText[cch] != L'\0')
        {
            cch++;
        }
    }
    return cch;
}

static SIZE_T StrLenA(const char* pszText)
{
    SIZE_T cb = 0;
    if (pszText)
    {
        while (pszText[cb] != '\0')
        {
            cb++;
        }
    }
    return cb;
}

BOOL RtEqualsIgnoreCaseW(LPCWSTR pwszA, LPCWSTR pwszB)
{
    if (!pwszA || !pwszB)
    {
        return FALSE;
    }
    return CompareStringOrdinal(pwszA, -1, pwszB, -1, TRUE) == CSTR_EQUAL;
}

BOOL RtStartsWithW(LPCWSTR pwszText, LPCWSTR pwszPrefix, BOOL bIgnoreCase)
{
    SIZE_T cchText = RtStrLenW(pwszText);
    SIZE_T cchPrefix = RtStrLenW(pwszPrefix);
    if (!pwszText || !pwszPrefix || cchText < cchPrefix || cchPrefix > 0x7FFFFFFF)
    {
        return FALSE;
    }
    return CompareStringOrdinal(pwszText, (int)cchPrefix, pwszPrefix, (int)cchPrefix, bIgnoreCase) == CSTR_EQUAL;
}

BOOL RtEndsWithIgnoreCaseW(LPCWSTR pwszText, LPCWSTR pwszSuffix)
{
    SIZE_T cchText = RtStrLenW(pwszText);
    SIZE_T cchSuffix = RtStrLenW(pwszSuffix);
    if (!pwszText || !pwszSuffix || cchText < cchSuffix || cchSuffix > 0x7FFFFFFF)
    {
        return FALSE;
    }
    return CompareStringOrdinal(pwszText + (cchText - cchSuffix), (int)cchSuffix, pwszSuffix, (int)cchSuffix, TRUE) == CSTR_EQUAL;
}

BOOL RtContainsCharW(LPCWSTR pwszText, WCHAR ch)
{
    if (!pwszText)
    {
        return FALSE;
    }
    for (; *pwszText != L'\0'; ++pwszText)
    {
        if (*pwszText == ch)
        {
            return TRUE;
        }
    }
    return FALSE;
}

LPWSTR RtConcatW(LPCWSTR pwszFirst, LPCWSTR pwszSecond)
{
    SIZE_T cchFirst = RtStrLenW(pwszFirst);
    SIZE_T cchSecond = RtStrLenW(pwszSecond);
    LPWSTR pwszResult = (LPWSTR)RtAlloc((cchFirst + cchSecond + 1) * sizeof(WCHAR));
    if (pwszResult)
    {
        if (cchFirst)
        {
            memcpy(pwszResult, pwszFirst, cchFirst * sizeof(WCHAR));
        }
        if (cchSecond)
        {
            memcpy(pwszResult + cchFirst, pwszSecond, cchSecond * sizeof(WCHAR));
        }
        pwszResult[cchFirst + cchSecond] = L'\0';
    }
    return pwszResult;
}

/* ------------------------------------------------------------------------------------------------
 * Output
 * ------------------------------------------------------------------------------------------------ */

static void WriteAll(HANDLE hOut, const char* pData, SIZE_T cbData)
{
    while (cbData > 0)
    {
        DWORD cbChunk = cbData > 0x40000000 ? 0x40000000 : (DWORD)cbData;
        DWORD cbWritten = 0;
        if (!WriteFile(hOut, pData, cbChunk, &cbWritten, NULL) || cbWritten == 0)
        {
            return;
        }
        pData += cbWritten;
        cbData -= cbWritten;
    }
}

void RtWriteUtf8(DWORD nStdHandle, const char* pText, SIZE_T cbText)
{
    HANDLE hStd = GetStdHandle(nStdHandle);
    if (!hStd || hStd == INVALID_HANDLE_VALUE || !pText || cbText == 0 || cbText > MAX_PAYLOAD_SIZE)
    {
        return;
    }

    DWORD dwMode = 0;
    if (!GetConsoleMode(hStd, &dwMode))
    {
        /* Redirected to a pipe or file: pass the UTF-8 bytes through unchanged */
        WriteAll(hStd, pText, cbText);
        return;
    }

    int cchWide = MultiByteToWideChar(CP_UTF8, 0, pText, (int)cbText, NULL, 0);
    if (cchWide <= 0)
    {
        return;
    }
    LPWSTR pwszText = (LPWSTR)RtAlloc((SIZE_T)cchWide * sizeof(WCHAR));
    if (!pwszText)
    {
        return;
    }
    MultiByteToWideChar(CP_UTF8, 0, pText, (int)cbText, pwszText, cchWide);

    SIZE_T cchOffset = 0;
    SIZE_T cchTotal = (SIZE_T)cchWide;
    while (cchOffset < cchTotal)
    {
        SIZE_T cchChunk = cchTotal - cchOffset;
        if (cchChunk > CONSOLE_WRITE_CHUNK)
        {
            cchChunk = CONSOLE_WRITE_CHUNK;
            /* Never split a surrogate pair across two writes */
            if (IS_HIGH_SURROGATE(pwszText[cchOffset + cchChunk - 1]))
            {
                cchChunk--;
            }
        }
        DWORD cchWritten = 0;
        if (!WriteConsoleW(hStd, pwszText + cchOffset, (DWORD)cchChunk, &cchWritten, NULL) || cchWritten == 0)
        {
            break;
        }
        cchOffset += cchWritten;
    }

    RtFree(pwszText);
}

void RtWriteString(DWORD nStdHandle, const char* pszText)
{
    RtWriteUtf8(nStdHandle, pszText, StrLenA(pszText));
}

/* ------------------------------------------------------------------------------------------------
 * JSON builder
 * ------------------------------------------------------------------------------------------------ */

static BOOL JsonReserve(JSON_BUILDER* pBuilder, SIZE_T cbExtra)
{
    if (pBuilder->bFailed)
    {
        return FALSE;
    }
    SIZE_T cbNeeded = pBuilder->cbLength + cbExtra + 1; /* keep room for a terminating NUL */
    if (cbNeeded > MAX_PAYLOAD_SIZE)
    {
        pBuilder->bFailed = TRUE;
        return FALSE;
    }
    if (cbNeeded <= pBuilder->cbCapacity)
    {
        return TRUE;
    }

    SIZE_T cbNewCapacity = pBuilder->cbCapacity ? pBuilder->cbCapacity : 256;
    while (cbNewCapacity < cbNeeded)
    {
        cbNewCapacity *= 2;
    }
    char* pNewData = (char*)RtReAlloc(pBuilder->pData, cbNewCapacity);
    if (!pNewData)
    {
        pBuilder->bFailed = TRUE;
        return FALSE;
    }
    pBuilder->pData = pNewData;
    pBuilder->cbCapacity = cbNewCapacity;
    return TRUE;
}

static void JsonAppendBytes(JSON_BUILDER* pBuilder, const char* pBytes, SIZE_T cbBytes)
{
    if (cbBytes == 0 || !JsonReserve(pBuilder, cbBytes))
    {
        return;
    }
    memcpy(pBuilder->pData + pBuilder->cbLength, pBytes, cbBytes);
    pBuilder->cbLength += cbBytes;
    pBuilder->pData[pBuilder->cbLength] = '\0';
}

void JsonAppendRaw(JSON_BUILDER* pBuilder, const char* pszRaw)
{
    JsonAppendBytes(pBuilder, pszRaw, StrLenA(pszRaw));
}

void JsonAppendStringUtf8(JSON_BUILDER* pBuilder, const char* pszUtf8)
{
    static const char HexDigits[] = "0123456789abcdef";

    JsonAppendBytes(pBuilder, "\"", 1);
    for (const unsigned char* p = (const unsigned char*)pszUtf8; p && *p; ++p)
    {
        unsigned char ch = *p;
        if (ch == '"' || ch == '\\')
        {
            char escaped[2] = { '\\', (char)ch };
            JsonAppendBytes(pBuilder, escaped, 2);
        }
        else if (ch < 0x20)
        {
            char escaped[6] = { '\\', 'u', '0', '0', HexDigits[ch >> 4], HexDigits[ch & 0xF] };
            JsonAppendBytes(pBuilder, escaped, 6);
        }
        else
        {
            /* Everything else, including multi-byte UTF-8 sequences, is valid inside a JSON string as-is */
            JsonAppendBytes(pBuilder, (const char*)p, 1);
        }
    }
    JsonAppendBytes(pBuilder, "\"", 1);
}

void JsonAppendStringW(JSON_BUILDER* pBuilder, LPCWSTR pwszText)
{
    if (!pwszText)
    {
        JsonAppendRaw(pBuilder, "null");
        return;
    }

    /* Invalid UTF-16 (lone surrogates) is replaced by U+FFFD, so the result is always valid UTF-8 */
    int cbUtf8 = WideCharToMultiByte(CP_UTF8, 0, pwszText, -1, NULL, 0, NULL, NULL);
    if (cbUtf8 <= 0)
    {
        pBuilder->bFailed = TRUE;
        return;
    }
    char* pszUtf8 = (char*)RtAlloc((SIZE_T)cbUtf8);
    if (!pszUtf8)
    {
        pBuilder->bFailed = TRUE;
        return;
    }
    WideCharToMultiByte(CP_UTF8, 0, pwszText, -1, pszUtf8, cbUtf8, NULL, NULL);
    JsonAppendStringUtf8(pBuilder, pszUtf8);
    RtFree(pszUtf8);
}

void JsonFree(JSON_BUILDER* pBuilder)
{
    RtFree(pBuilder->pData);
    pBuilder->pData = NULL;
    pBuilder->cbLength = 0;
    pBuilder->cbCapacity = 0;
}
