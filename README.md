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


## License

This software is provided under the MIT license. For hobby and research purposes, use it according to the MIT license.

For industrial applications, be careful with patents.

This library is MIT licensed open source software, but not patent free.


## GetStarted


### Setup with Getstarted.
Clone the sorce code from github.

```
>git clone https://github.com/nyatla/TBSKmodemCS.git
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



