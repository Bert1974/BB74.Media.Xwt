#include "stdafx.h"

debugstringfunction g_callback = 0;

extern "C" {
	FUNCEXP void __setprintf(debugstringfunction callback)
	{
		g_callback = callback;
	}
}
FUNCEXP void __printf(std::string fmt, ...)
{
	if (g_callback)
	{
		int size = ((int)fmt.size()) * 2 + 50;   // Use a rubric appropriate for your code
		std::string str;
		va_list ap;
		while (1) {     // Maximum two passes on a POSIX system...
			str.resize(size);
			va_start(ap, fmt);
			int n = vsnprintf((char *)str.data(), size, fmt.c_str(), ap);
			va_end(ap);
			if (n > -1 && n < size) {  // Everything worked
				str.resize(n);
				break;
			}
			if (n > -1)  // Needed size returned
				size = n + 1;   // For null char
			else
				size *= 2;      // Guess at a larger size (OS specific)
		}
		g_callback(str.c_str());
	}
}
