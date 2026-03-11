using System;

namespace TqkLibrary.AudioCapture.Enums
{
    /// <summary>
    /// Specifies the format tag for audio capture.
    /// </summary>
    public enum AudioFormatTag : int
    {
        /// <summary>
        /// Hardware/OS default format (usually IEEE Float from GetMixFormat). Used when custom format is not applied.
        /// </summary>
        Default = 0,

        /// <summary>
        /// WAVE_FORMAT_PCM. Standard integer PCM format.
        /// </summary>
        PCM = 1,

        /// <summary>
        /// WAVE_FORMAT_IEEE_FLOAT. 32-bit floating point format.
        /// </summary>
        IEEE_FLOAT = 3
    }
}
