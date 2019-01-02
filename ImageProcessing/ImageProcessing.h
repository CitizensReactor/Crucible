#pragma once

using namespace System;

namespace ImageProcessing {
	public ref class DDS
	{
	public:
		static array<Byte>^ Convert(array<Byte>^ input, bool normal);
	};
}
