﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace CSharpGL
{
    /// <summary>
    /// helper class.
    /// </summary>
    public static class FontBitmapHelper
    {
        /// <summary>
        /// Gets a <see cref="FontBitmap"/>'s intance.
        /// </summary>
        /// <param name="font"></param>
        /// <param name="charSet"></param>
        /// <returns></returns>
        public static FontBitmap GetFontBitmap(this Font font, string charSet)
        {
            var fontBitmap = new FontBitmap();
            fontBitmap.GlyphFont = font;

            // 以下三步，不能调换先后顺序。
            // Don't change the order in which these 3 functions invoked.
            GetGlyphSizes(fontBitmap, charSet);
            int width, height;
            GetGlyphPositions(fontBitmap, charSet, out width, out height);
            PrintBitmap(fontBitmap, charSet, width, height);
            RetargetGlyphRectangleInwards(fontBitmap);
            //using (var graphics = Graphics.FromImage(fontBitmap.GlyphBitmap))
            //{
            //    foreach (var item in fontBitmap.GlyphInfoDictionary.Values)
            //    {
            //        graphics.DrawRectangle(Pens.Red, item.ToRectangle());
            //    }
            //}
            fontBitmap.GlyphBitmap.Save("TestFontBitmap.bmp");
            //var fontResource = new FontResource();
            //fontResource.FontHeight = pixelSize + yInterval;
            //fontResource.CharInfoDict = dict;
            //fontResource.InitTexture(finalBitmap);
            //finalBitmap.Dispose();
            //return fontResource;
            return fontBitmap;
        }

        private static void PrintBitmap(FontBitmap fontBitmap, string charSet, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                foreach (KeyValuePair<char, GlyphInfo> item in fontBitmap.GlyphInfoDictionary)
                {
                    graphics.DrawString(item.Key.ToString(), fontBitmap.GlyphFont, Brushes.White, item.Value.xoffset, item.Value.yoffset);
                    // draw a tectangle that displays glyph's area.
                    //graphics.DrawRectangle(Pens.Red, item.Value.xoffset, item.Value.yoffset, item.Value.width, item.Value.height);
                }
            }

            fontBitmap.GlyphBitmap = bitmap;
        }

        private static void RetargetGlyphRectangleInwards(FontBitmap fontBitmap)
        {
            var minYOffset = int.MaxValue;

            Bitmap initialBmp = fontBitmap.GlyphBitmap;
            BitmapData initialBitmapData = initialBmp.LockBits(new Rectangle(0, 0, initialBmp.Width, initialBmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            foreach (var glyph in fontBitmap.GlyphInfoDictionary.Values)
            {
                RetargetGlyphRectangleInwards(initialBitmapData, glyph);
                minYOffset = Math.Min(minYOffset, glyph.yoffset);
            }
            initialBmp.UnlockBits(initialBitmapData);

            minYOffset--; // give one pixel of breathing room?

            //foreach (var glyph in fontBitmap.GlyphInfoDictionary.Values)
            //    glyph.yoffset -= minYOffset;
        }

        /// <summary>
        /// Returns try if the given pixel is empty (i.e. black)
        /// </summary>
        private static unsafe bool EmptyPixel(BitmapData bitmapData, int px, int py)
        {
            byte* addr = (byte*)(bitmapData.Scan0) + bitmapData.Stride * py + px * 3;
            return (*addr == 0 && *(addr + 1) == 0 && *(addr + 2) == 0);
        }

        private static void RetargetGlyphRectangleInwards(BitmapData bitmapData, GlyphInfo glyph)
        {
            int startX, endX;
            int startY, endY;

            for (startX = glyph.xoffset; startX < bitmapData.Width; startX++)
                for (int j = glyph.yoffset; j < glyph.yoffset + glyph.height; j++)
                    if (!EmptyPixel(bitmapData, startX, j))
                        goto Done1;
        Done1:
            for (endX = glyph.xoffset + glyph.width - 1; endX >= 0; endX--)
                for (int j = glyph.yoffset; j < glyph.yoffset + glyph.height; j++)
                    if (!EmptyPixel(bitmapData, endX, j))
                        goto Done2;
        Done2:
            for (startY = glyph.yoffset; startY < bitmapData.Height; startY++)
                for (int i = startX; i < endX; i++)
                    if (!EmptyPixel(bitmapData, i, startY))
                        goto Done3;

        Done3:
            for (endY = glyph.yoffset + glyph.height - 1; endY >= 0; endY--)
                for (int i = startX; i < endX; i++)
                    if (!EmptyPixel(bitmapData, i, endY))
                        goto Done4;
        Done4: ;

            if (endY < startY)
                startY = endY = glyph.yoffset;

            if (endX < startX)
                startX = endX = glyph.xoffset;

            glyph.xoffset = startX;
            //glyph.yoffset = startY;
            glyph.width = endX - startX + 1;
            //glyph.height = endY - startY + 1;
        }

        private static void GetGlyphPositions(FontBitmap fontBitmap, string charSet, out int width, out int height)
        {
            int sideLength;
            int maxGlyphHeight = 0;
            {
                float totalWidth = 0.0f;
                foreach (GlyphInfo item in fontBitmap.GlyphInfoDictionary.Values)
                {
                    totalWidth += item.width;
                    if (maxGlyphHeight < item.height) { maxGlyphHeight = item.height; }
                }
                float area = totalWidth * maxGlyphHeight;
                sideLength = (int)Math.Ceiling(Math.Sqrt(area));
                fontBitmap.GlyphHeight = maxGlyphHeight;
            }
            {
                int maxWidth = 0, maxHeight = 0;
                int currentX = 0, currentY = 0;
                foreach (var item in fontBitmap.GlyphInfoDictionary)
                {
                    if (currentX + item.Value.width < sideLength)
                    {
                        item.Value.xoffset = currentX;
                        item.Value.yoffset = currentY;
                        currentX += item.Value.width;
                    }
                    else
                    {
                        if (maxWidth < currentX) { maxWidth = currentX; }
                        currentX = 0;
                        currentY += maxGlyphHeight;
                        item.Value.xoffset = currentX;
                        item.Value.yoffset = currentY;
                        currentX += item.Value.width;
                    }
                }
                maxHeight = currentY + maxGlyphHeight;
                width = maxWidth;
                height = maxHeight;
            }
        }

        private static void GetGlyphSizes(FontBitmap fontBitmap, string charSet)
        {
            float fontSize = fontBitmap.GlyphFont.Size;

            using (var bitmap = new Bitmap(1, 1, PixelFormat.Format24bppRgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    foreach (char c in charSet)
                    {
                        SizeF size = graphics.MeasureString(c.ToString(), fontBitmap.GlyphFont);
                        // glyph's position is not settled yet.
                        var info = new GlyphInfo(0, 0, (int)size.Width, (int)size.Height);
                        fontBitmap.GlyphInfoDictionary.Add(c, info);
                    }
                }
            }
        }

    }
}