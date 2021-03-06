﻿using System;
using System.Collections.Generic;

namespace CSharpGL
{
    internal class OneIndexPointInTriangleSearcher : OneIndexPointSearcher
    {
        internal override uint Search(RenderEventArgs arg,
            int x, int y,
            RecognizedPrimitiveInfo primitiveInfo,
            OneIndexRenderer modernRenderer)
        {
            uint[] indexList = primitiveInfo.VertexIds;
            if (indexList.Length != 3) { throw new ArgumentException(); }

            OneIndexBufferPtr indexBufferPtr = null;
            using (var buffer = new OneIndexBuffer(IndexElementType.UInt, DrawMode.Points, BufferUsage.StaticDraw))
            {
                buffer.Create(3);
                unsafe
                {
                    var array = (uint*)buffer.Header.ToPointer();
                    array[0] = indexList[0];
                    array[1] = indexList[1];
                    array[2] = indexList[2];
                }

                indexBufferPtr = buffer.GetBufferPtr() as OneIndexBufferPtr;
            }

            modernRenderer.Render4InnerPicking(arg, indexBufferPtr);
            uint id = ColorCodedPicking.ReadPixel(x, arg.CanvasRect.Height - y - 1);

            indexBufferPtr.Dispose();

            if (id == indexList[0] || id == indexList[1] || id == indexList[2])
            { return id; }
            else
            { throw new Exception("This should not happen!"); }
        }
    }
}