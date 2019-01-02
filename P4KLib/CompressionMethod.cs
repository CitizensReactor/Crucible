using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P4KLib
{
    enum CompressionMethod
    {
        NoCompression = 00,             // no compression
        Shrunk = 01,                    // shrunk
        ReducedCompressionFactor1 = 02, // reduced with compression factor 1
        ReducedCompressionFactor2 = 03, // reduced with compression factor 2
        ReducedCompressionFactor3 = 04, // reduced with compression factor 3
        ReducedCompressionFactor4 = 05, // reduced with compression factor 4
        Imploded = 06,                  // imploded
        _Reserved7 = 07,                // reserved
        Deflated = 08,                  // deflated
        EnhancedDeflated = 09,          // enhanced deflated
        PKWare_DCL_Imploded = 10,       // PKWare DCL imploded
        _ReservedBit11 = 11,            // reserved
        BZIP2 = 12,                     // compressed using BZIP2
        _ReservedBit13 = 13,            // reserved
        LZMA = 14,                      // LZMA
        _Reserved15,                   // reserved
        _Reserved16,                   // reserved
        _Reserved17,                   // reserved
        IBM_TERSE = 18,                 // compressed using IBM TERSE
        IBM_LZ77 = 19,                  // IBM LZ77 z
        PPMd_Version_I = 98,            // PPMd version I, Rev 1 

        // Cloud Imperium Games
        ZStd = 100
    }
}
