#include "stdafx.h"

#include <DirectXTex/DirectXTex.h>

#include "ImageProcessing.h"

using namespace DirectX;

struct RGBA8_SNORM_PIXEL
{
	SByte R;
	SByte G;
	SByte B;
	SByte A;
};

struct RGBA8_UNORM_PIXEL
{
	Byte R;
	Byte G;
	Byte B;
	Byte A;
};

void BC5toRGBNormal(void* data, size_t data_length)
{
	size_t num_pixels = data_length / sizeof(RGBA8_SNORM_PIXEL);
	const RGBA8_SNORM_PIXEL* spixels = reinterpret_cast<const RGBA8_SNORM_PIXEL*>(data);
	RGBA8_UNORM_PIXEL* upixels = reinterpret_cast<RGBA8_UNORM_PIXEL*>(data);

	for (int i = 0; i < num_pixels; i++)
	{
		auto &pixel = spixels[i];

		float r = float(pixel.R) / (pixel.R > 0 ? 127.0f : 128.0f);
		float g = float(pixel.G) / (pixel.G > 0 ? 127.0f : 128.0f);

		//float b = 1.0 - sqrtf(r * r + g * g);

		//float r = float(pixel.R) / (255.0f);
		//float g = float(pixel.G) / (255.0f);

		//r = r * 2.0f - 1.0f;
		//g = g * 2.0f - 1.0f;

		float r2 = r * r;
		float g2 = g * g;

		float z = 1.0f - sqrt(r2 + g2);
		float length = sqrtf(r2 + g2 + z * z);

		r /= length;
		g /= length;
		z /= length;

		//pixel.R = Byte(255.0f * r);
		//pixel.G = Byte(255.0f * g);
		//pixel.B = Byte(255.0f * z);

		r = (r + 1.0f) * 0.5f;
		g = (g + 1.0f) * 0.5f;
		z = (z + 1.0f) * 0.5f;

		RGBA8_UNORM_PIXEL upixel;
		upixel.R = Byte(255.0f * r);
		upixel.G = Byte(255.0f * g);
		upixel.B = Byte(255.0f * z);
		upixel.A = Byte(255.0f * 1.0f);

		upixels[i] = upixel;
	}
}

array<Byte>^ ImageProcessing::DDS::Convert(array<Byte>^ input, bool normal)
{
	HRESULT coInitializeExResult = CoInitializeEx(nullptr, COINITBASE_MULTITHREADED);
	if (FAILED(coInitializeExResult))
	{
		throw 1;
	}

	if (input->Length == 0)
	{
		return nullptr;
	}

	pin_ptr<Byte> input_pinned_ptr = &input[0];
	Byte* input_ptr = input_pinned_ptr;

	ScratchImage ddsScratchImage;
	HRESULT loadFromDDSMemoryResult = LoadFromDDSMemory(input_ptr, input->Length, 0, nullptr, ddsScratchImage);
	if (FAILED(loadFromDDSMemoryResult))
	{
		throw 1;
	}
	const Image& ddsImage = *ddsScratchImage.GetImage(0, 0, 0);

	bool compressed = false;

	switch (ddsImage.format)
	{
	case DXGI_FORMAT_UNKNOWN: //0,
	case DXGI_FORMAT_R32G32B32A32_TYPELESS: //1,
	case DXGI_FORMAT_R32G32B32A32_FLOAT: //2,
	case DXGI_FORMAT_R32G32B32A32_UINT: //3,
	case DXGI_FORMAT_R32G32B32A32_SINT: //4,
	case DXGI_FORMAT_R32G32B32_TYPELESS: //5,
	case DXGI_FORMAT_R32G32B32_FLOAT: //6,
	case DXGI_FORMAT_R32G32B32_UINT: //7,
	case DXGI_FORMAT_R32G32B32_SINT: //8,
	case DXGI_FORMAT_R16G16B16A16_TYPELESS: //9,
	case DXGI_FORMAT_R16G16B16A16_FLOAT: //10,
	case DXGI_FORMAT_R16G16B16A16_UNORM: //11,
	case DXGI_FORMAT_R16G16B16A16_UINT: //12,
	case DXGI_FORMAT_R16G16B16A16_SNORM: //13,
	case DXGI_FORMAT_R16G16B16A16_SINT: //14,
	case DXGI_FORMAT_R32G32_TYPELESS: //15,
	case DXGI_FORMAT_R32G32_FLOAT: //16,
	case DXGI_FORMAT_R32G32_UINT: //17,
	case DXGI_FORMAT_R32G32_SINT: //18,
	case DXGI_FORMAT_R32G8X24_TYPELESS: //19,
	case DXGI_FORMAT_D32_FLOAT_S8X24_UINT: //20,
	case DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS: //21,
	case DXGI_FORMAT_X32_TYPELESS_G8X24_UINT: //22,
	case DXGI_FORMAT_R10G10B10A2_TYPELESS: //23,
	case DXGI_FORMAT_R10G10B10A2_UNORM: //24,
	case DXGI_FORMAT_R10G10B10A2_UINT: //25,
	case DXGI_FORMAT_R11G11B10_FLOAT: //26,
	case DXGI_FORMAT_R8G8B8A8_TYPELESS: //27,
	case DXGI_FORMAT_R8G8B8A8_UNORM: //28,
	case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB: //29,
	case DXGI_FORMAT_R8G8B8A8_UINT: //30,
	case DXGI_FORMAT_R8G8B8A8_SNORM: //31,
	case DXGI_FORMAT_R8G8B8A8_SINT: //32,
	case DXGI_FORMAT_R16G16_TYPELESS: //33,
	case DXGI_FORMAT_R16G16_FLOAT: //34,
	case DXGI_FORMAT_R16G16_UNORM: //35,
	case DXGI_FORMAT_R16G16_UINT: //36,
	case DXGI_FORMAT_R16G16_SNORM: //37,
	case DXGI_FORMAT_R16G16_SINT: //38,
	case DXGI_FORMAT_R32_TYPELESS: //39,
	case DXGI_FORMAT_D32_FLOAT: //40,
	case DXGI_FORMAT_R32_FLOAT: //41,
	case DXGI_FORMAT_R32_UINT: //42,
	case DXGI_FORMAT_R32_SINT: //43,
	case DXGI_FORMAT_R24G8_TYPELESS: //44,
	case DXGI_FORMAT_D24_UNORM_S8_UINT: //45,
	case DXGI_FORMAT_R24_UNORM_X8_TYPELESS: //46,
	case DXGI_FORMAT_X24_TYPELESS_G8_UINT: //47,
	case DXGI_FORMAT_R8G8_TYPELESS: //48,
	case DXGI_FORMAT_R8G8_UNORM: //49,
	case DXGI_FORMAT_R8G8_UINT: //50,
	case DXGI_FORMAT_R8G8_SNORM: //51,
	case DXGI_FORMAT_R8G8_SINT: //52,
	case DXGI_FORMAT_R16_TYPELESS: //53,
	case DXGI_FORMAT_R16_FLOAT: //54,
	case DXGI_FORMAT_D16_UNORM: //55,
	case DXGI_FORMAT_R16_UNORM: //56,
	case DXGI_FORMAT_R16_UINT: //57,
	case DXGI_FORMAT_R16_SNORM: //58,
	case DXGI_FORMAT_R16_SINT: //59,
	case DXGI_FORMAT_R8_TYPELESS: //60,
	case DXGI_FORMAT_R8_UNORM: //61,
	case DXGI_FORMAT_R8_UINT: //62,
	case DXGI_FORMAT_R8_SNORM: //63,
	case DXGI_FORMAT_R8_SINT: //64,
	case DXGI_FORMAT_A8_UNORM: //65,
	case DXGI_FORMAT_R1_UNORM: //66,
	case DXGI_FORMAT_R9G9B9E5_SHAREDEXP: //67,
	case DXGI_FORMAT_R8G8_B8G8_UNORM: //68,
	case DXGI_FORMAT_G8R8_G8B8_UNORM: //69,
	case DXGI_FORMAT_B5G6R5_UNORM: //85,
	case DXGI_FORMAT_B5G5R5A1_UNORM: //86,
	case DXGI_FORMAT_B8G8R8A8_UNORM: //87,
	case DXGI_FORMAT_B8G8R8X8_UNORM: //88,
	case DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM: //89,
	case DXGI_FORMAT_B8G8R8A8_TYPELESS: //90,
	case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB: //91,
	case DXGI_FORMAT_B8G8R8X8_TYPELESS: //92,
	case DXGI_FORMAT_B8G8R8X8_UNORM_SRGB: //93,
	case DXGI_FORMAT_AYUV: //100,
	case DXGI_FORMAT_Y410: //101,
	case DXGI_FORMAT_Y416: //102,
	case DXGI_FORMAT_NV12: //103,
	case DXGI_FORMAT_P010: //104,
	case DXGI_FORMAT_P016: //105,
	case DXGI_FORMAT_420_OPAQUE: //106,
	case DXGI_FORMAT_YUY2: //107,
	case DXGI_FORMAT_Y210: //108,
	case DXGI_FORMAT_Y216: //109,
	case DXGI_FORMAT_NV11: //110,
	case DXGI_FORMAT_AI44: //111,
	case DXGI_FORMAT_IA44: //112,
	case DXGI_FORMAT_P8: //113,
	case DXGI_FORMAT_A8P8: //114,
	case DXGI_FORMAT_B4G4R4A4_UNORM: //115,
		break;
	case DXGI_FORMAT_BC1_TYPELESS: //70,
	case DXGI_FORMAT_BC1_UNORM: //71,
	case DXGI_FORMAT_BC1_UNORM_SRGB: //72,
	case DXGI_FORMAT_BC2_TYPELESS: //73,
	case DXGI_FORMAT_BC2_UNORM: //74,
	case DXGI_FORMAT_BC2_UNORM_SRGB: //75,
	case DXGI_FORMAT_BC3_TYPELESS: //76,
	case DXGI_FORMAT_BC3_UNORM: //77,
	case DXGI_FORMAT_BC3_UNORM_SRGB: //78,
	case DXGI_FORMAT_BC4_TYPELESS: //79,
	case DXGI_FORMAT_BC4_UNORM: //80,
	case DXGI_FORMAT_BC4_SNORM: //81,
	case DXGI_FORMAT_BC5_TYPELESS: //82,
	case DXGI_FORMAT_BC5_UNORM: //83,
	case DXGI_FORMAT_BC5_SNORM: //84,
	case DXGI_FORMAT_BC6H_TYPELESS: //94,
	case DXGI_FORMAT_BC6H_UF16: //95,
	case DXGI_FORMAT_BC6H_SF16: //96,
	case DXGI_FORMAT_BC7_TYPELESS: //97,
	case DXGI_FORMAT_BC7_UNORM: //98,
	case DXGI_FORMAT_BC7_UNORM_SRGB: //99,
		compressed = true;
		break;
	default:
	case DXGI_FORMAT_P208: //130,
	case DXGI_FORMAT_V208: //131,
	case DXGI_FORMAT_V408: //132,
		throw gcnew NotSupportedException();
	}

	auto format = normal ? DXGI_FORMAT_R8G8B8A8_SNORM : DXGI_FORMAT_R8G8B8A8_UNORM;

	ScratchImage scratchImage;

	if (compressed)
	{
		HRESULT convertResult = DirectX::Decompress(ddsImage, format, scratchImage);
		if (FAILED(convertResult))
		{
			auto message = "Failed to decompress block image 0x" + convertResult.ToString("X");
			throw gcnew Exception(message);
		}
	}
	else
	{
		HRESULT convertResult = DirectX::Convert(ddsImage, format, TEX_FILTER_DEFAULT, TEX_THRESHOLD_DEFAULT, scratchImage);
		if (FAILED(convertResult))
		{
			throw 1;
		}

	}
	const Image& sourceImage = *scratchImage.GetImage(0, 0, 0);

	if (normal)
	{
		BC5toRGBNormal(sourceImage.pixels, sourceImage.rowPitch * sourceImage.height);
		// hack the format back to UNORM
		((Image*)&sourceImage)->format = DXGI_FORMAT_R8G8B8A8_UNORM;
	}

	Blob dataBlob;
	HRESULT saveToWICMemoryResult = SaveToWICMemory(sourceImage, WIC_FLAGS_NONE, GetWICCodec(WIC_CODEC_TIFF), dataBlob);
	if (FAILED(saveToWICMemoryResult))
	{
		auto message = "Failed to save WIC to memory 0x" + saveToWICMemoryResult.ToString("X");
		throw gcnew Exception(message);
	}

	auto dataPtr = dataBlob.GetBufferPointer();
	auto dataSize = dataBlob.GetBufferSize();

	array<Byte>^ outputData = gcnew array<Byte>((int)dataSize);
	pin_ptr<Byte> outputDataPinnedPtr = &outputData[0];
	Byte* outputDataPtr = outputDataPinnedPtr;

	memcpy(outputDataPtr, dataPtr, dataSize);

	return outputData;
}
