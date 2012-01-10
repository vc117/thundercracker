﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace PNGtoMask {
  class Program {

    enum TransparencyType {
      AllTransparent = 0,
      AllOpaque = 1,
      SomeTransparent = 2,
      NumTransparencyTypes
    }

    static void Main(string[] args) {
      // Open a Stream and decode a PNG image
      string[] inputFileNames = { 
        @"..\\..\\..\\wc_letters_connected_center.png",
        @"..\\..\\..\\wc_letters_connected_center_2.png",
        @"..\\..\\..\\wc_letters_connected_left.png",
        @"..\\..\\..\\wc_letters_connected_left_2.png",
        @"..\\..\\..\\wc_letters_connected_right.png",
        @"..\\..\\..\\WC_letters_connected_right_2.png",
        @"..\\..\\..\\wc_letters_neighbored.png",
        @"..\\..\\..\\wc_transition.png",
      };

      using (var outputFile = new System.IO.StreamWriter("..\\..\\..\\TileTransparencyLookupData.cpp")) {
        outputFile.WriteLine("// This file is generated by PNGtoMask, do not hand edit");
        outputFile.WriteLine("#include <sifteo.h>");
        outputFile.WriteLine("#include \"TileTransparencyLookup.h\"");
        outputFile.WriteLine();        

        for (int inputFileIndex = 0; inputFileIndex < inputFileNames.Length; ++inputFileIndex) {
          string inputFileName = inputFileNames[inputFileIndex];
          List<byte> outputList = new List<byte>();
          using (Bitmap image = new Bitmap(inputFileName, false)) {

            int numTiles = 16; // assumes 128x128 full screen image
            int frameSize = 128;
            const int TILE_SIZE = 8;
            byte output = 0;
            int outputShift = 0;
            int outputTileBits = 2; // bits
            int maxOutputShift = 8 - outputTileBits;

            // Loop through the images pixels to reset color.
            for (int frame = 0; frame * frameSize < image.Width; ++frame) {
              int someTransparentCount = 0;
              for (int tileY = 0; tileY < numTiles; ++tileY) {
                for (int tileX = 0; tileX < numTiles; ++tileX) {

                  bool alpha = false;
                  bool opaque = false;
                  int startX = frame * frameSize + tileX * TILE_SIZE;
                  for (int x = startX; x < startX + TILE_SIZE; ++x) {

                    int startY = tileY * TILE_SIZE;
                    for (int y = startY; y < startY + TILE_SIZE; ++y) {
                      Color pixelColor = image.GetPixel(x, y);
                      if (pixelColor.A < 0x80) {
                          // alphaThreshold from tile.cpp in STIR code
                          alpha = true;
                      }
                      else {
                          opaque = true;
                      }
                    }
                  }
                  TransparencyType bits;
                  if (alpha && !opaque) {
                    bits = TransparencyType.AllTransparent;
                  }
                  else if (!alpha && opaque) {
                    bits = TransparencyType.AllOpaque;
                  }
                  else {
                    bits = TransparencyType.SomeTransparent; // alpha && opqaue or !alpha && !opaque
                    ++someTransparentCount;
                    Debug.Assert(someTransparentCount <= 144, "Too many transparent tiles for BG1");
                  }
                  //Console.Out.WriteLine("frame {0}, tile ({1}, {2}), {3}", frame, tileX, tileY, bits.ToString());
                  output |= (byte)((int)bits << outputShift);
                  outputShift += outputTileBits;
                  if (outputShift > maxOutputShift) {
                    // save byte
                    outputList.Add(output);
                    //Console.Out.WriteLine("byte 0x{0}", output.ToString("x"));
                    outputShift = 0;
                    output = 0;
                  }
                }
              }
            }
          }
          outputFile.WriteLine("const static uint8_t tileTransparencyLookupData{0}[] =\t//{1}", inputFileIndex, inputFileName);
          outputFile.WriteLine("{");


          for (int i = 0; i < outputList.Count; ++i) {
            if (i % 4 == 0) {
              outputFile.Write("\t");
            }
            outputFile.Write("0x" + outputList[i].ToString("x"));
            outputFile.Write(", ");
            if ((i % 4) == 3) {
              outputFile.WriteLine();
            }
          }
          outputFile.WriteLine("};");
          outputFile.WriteLine();

          //Console.Out.WriteLine(outputList.ToString());
        }

        outputFile.WriteLine("const uint8_t* tileTransparencyLookupData[NumImageIndexes] =");
        outputFile.WriteLine("{");


        for (int i = 0; i < inputFileNames.Length; ++i) {
          outputFile.WriteLine("\ttileTransparencyLookupData{0},",i);
        }
        outputFile.WriteLine("};");
        outputFile.WriteLine();
        outputFile.Close();

      }
    }
  }
}
