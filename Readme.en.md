# TBSK modem for C#

Japanese documente 👉[Readme.md](Readme.md)

This is pure C# implementation of TBSKmodem.
🐓[TBSKmodem](https://github.com/nyatla/TBSKmodem.en)


TBSK (Trait Block Shift Keying) modem is a low-speed, short-range audio communication implementation without FFT/IFTT.

It can modulate a byte/bitstream to PCM  and demodulate PCM to a byte/bitstream.

There is a library for development and a console script [tbskmodem](tbskmodem.md).

![preview_tbsk](https://user-images.githubusercontent.com/2483108/194768184-cecddff0-1fa4-4df8-af3f-f16ed4ef1718.gif)

See [Youtube](https://www.youtube.com/watch?v=4cB3hWATDUQ) with modulated sound.🎵

※This is python demonstration.


## Performance

Throughput in a quiet room is 5 bps to 1 kbps and transmit distance is about 1 meter.
It is possible with the microphone and speakers provided in a personal computer.


## Specification

| Parameters |  |
| --- | --- |
| Modulation method | Trait block differential modulation |
| Bit rate | 5 - 1kbps |
| Carrier frequency | Any |
| Bandwidths | 5Hz -  |
| Error correction/detection | No |

### Trait block differential modulation

Trait block differential modulation uses an any shaped tone signal and its inverted value as a binary transmission symbol instead of a waveform symbol.
The tone signal is a spread-spectrum Sin wave, but other shaped waveforms can be used.
Demodulation is performed by delay detection of the correlation rate of adjacent symbols. The correlation rate indicates  1,-1, which is demodulated into bits.

This modulatlation method has the only one parameter that is tone signal length (number of ticks x carrier frequency). The demodulator can demodulate any type of signal as long as the tone signal length is compatible.

### Signal Synchronization

First signal detection is determined by observing the correlation value for a certain period of time. A first synchronization pattern longer than a normal symbol, it is placed at the head of the signal.
And to maintain the state of synchronization, demodulator  uses the edge of the symbol in the signal  to detect the peak of the correlation.

If a signal is sent with symbols not inverted for a long time in an unstable carrier wave system, the transaction will be interrupted by lack of synchronization.
Should be good to process the data to be transmitted so that the data is inverted once every few seconds.

### Tone Signal

Default tone signal is a spread spectrum waveform with a sine wave phase-shifted by a PN code.
The tone signal can be any shape that is a high signal-to-noise ratio on the demodulation side. If the tone signal is sine, it behaves the same as DPSK modulation.

### Disturbance Tolerance

Disturbance tolerance becomes stronger the longer the tone signal, but lower the bit rate if longer the tone signal, 
The communication rate relative to the carrier frequency is 0.01 bit/Hz is the realstic.


### Packet format
The current protocol only implements signal detection and followed payload reading. Applications should implement packet size, termination identifier, error correction, and detection.

## License

This software is provided under the MIT license. For hobby and research purposes, use it according to the MIT license.

For industrial applications, be careful with patents.

This library is MIT licensed open source software, but not patent free.


## GetStarted


### Setup with Getstarted.
Clone the sorce code from github.

```
>git clone https://github.com/nyatla/TBSKmodemVS.git
```

TBSKmodemCS has some getstarted samples project. Open TBSKmodem.sln and click getstarted folder.

Check to the python version for the explanation of the sample.

[TBSKmodem#Location of sample scripts](https://github.com/nyatla/TBSKmodem#%E3%82%B5%E3%83%B3%E3%83%97%E3%83%AB%E3%83%97%E3%83%AD%E3%82%B0%E3%83%A9%E3%83%A0%E3%81%AE%E5%A0%B4%E6%89%80)


1. step1_modulate - Modulate to wave file
2. step2_demodulate - Demodulate from wav file
3. step3_bytedata - Modulate and demodulate byte data
4. step4_text - Modulate and demodulate text
5. step5_microphone - Testing microphone
6. step6_realtime_receive - Realtime demodulation



