namespace TqkLibrary.AudioCapture.Models
{
    public class AudioFormat
    {
        public int Channels { get; init; }
        public int SampleRate { get; init; }
        public int BitsPerSample { get; init; }
        public int BlockAlign => Channels * (BitsPerSample / 8);
        public int BytesPerSecond => SampleRate * BlockAlign;

        public override string ToString() => $"{Channels}ch, {SampleRate}Hz, {BitsPerSample}bit";
    }
}
