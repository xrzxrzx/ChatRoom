#ifdef _WIN32
#include <windows.h>
#endif

#include<string>

static std::string ToUtf8(const std::string& input)
{
#ifdef _WIN32
    if (input.empty()) return {};

    int wideLen = MultiByteToWideChar(CP_ACP, 0, input.c_str(), -1, nullptr, 0);
    if (wideLen <= 0) return input;

    std::wstring wide(wideLen - 1, L'\0');
    MultiByteToWideChar(CP_ACP, 0, input.c_str(), -1, wide.data(), wideLen);

    int utf8Len = WideCharToMultiByte(CP_UTF8, 0, wide.c_str(), -1, nullptr, 0, nullptr, nullptr);
    if (utf8Len <= 0) return input;

    std::string utf8(utf8Len - 1, '\0');
    WideCharToMultiByte(CP_UTF8, 0, wide.c_str(), -1, utf8.data(), utf8Len, nullptr, nullptr);
    return utf8;
#else
    return input;
#endif
}