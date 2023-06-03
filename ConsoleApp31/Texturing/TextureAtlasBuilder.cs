using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31.Texturing;
internal class TextureAtlasBuilder
{
    private List<Color[]> rows = new();
    private Dictionary<BlockID, int> indices = new();
    private Dictionary<string, ImageResult> imageCache = new();
    private int nextIndex;

    public void AddBlock(BlockID id, BlockFaces faces)
    {

        Color[] colors = new Color[TextureAtlas.TileWidth * TextureAtlas.TileHeight * TextureAtlas.AtlasWidth];

        var top = LoadImageColors(faces.Top);
        CopyImage(top, 0);

        var bottom = LoadImageColors(faces.Bottom);
        CopyImage(bottom, 1);

        var forward = LoadImageColors(faces.Forward);
        CopyImage(forward, 2);

        var right = LoadImageColors(faces.Right);
        CopyImage(right, 3);

        var backward = LoadImageColors(faces.Backward);
        CopyImage(backward, 4);

        var left = LoadImageColors(faces.Left);
        CopyImage(left, 5);

        indices.Add(id, nextIndex++);
        rows.Add(colors);

        void CopyImage(Span<Color> image, int offset)
        {
            Debug.Assert(image.Length == TextureAtlas.TileWidth * TextureAtlas.TileHeight);

            for (int i = 0; i < TextureAtlas.TileHeight; i++)
            {
                var srcRow = image.Slice(i * TextureAtlas.TileWidth, TextureAtlas.TileWidth);
                var dstRow = colors.AsSpan(i * TextureAtlas.AtlasWidth * TextureAtlas.TileWidth + offset * TextureAtlas.TileWidth, TextureAtlas.TileWidth);

                srcRow.CopyTo(dstRow);
            }
        }
    }

    private Span<Color> LoadImageColors(string image)
    {
        if (!imageCache.TryGetValue(image, out var result))
        {
            using var fs = new FileStream(image, FileMode.Open);
            result = ImageResult.FromStream(fs, ColorComponents.RedGreenBlueAlpha);
            imageCache.Add(image, result);
        }

        return MemoryMarshal.Cast<byte, Color>(result.Data.AsSpan());
    }

    public TextureAtlas Finish()
    {
        return new TextureAtlas(indices, rows);
    }
}

class TextureAtlas
{
    public const int TileWidth = 16;
    public const int TileHeight = 16;
    public const int AtlasWidth = 6;

    public ImageTexture Texture { get; private set; }

    private Dictionary<BlockID, int> indices;
    private int rowCount;

    internal TextureAtlas(Dictionary<BlockID, int> indices, List<Color[]> rows)
    {
        this.indices = indices;
        this.rowCount = rows.Count;
        
        Texture = new(TileWidth * AtlasWidth, TileHeight * rows.Count);

        for (int i = 0; i < rows.Count; i++)
        {
            Texture.UpdateRegion(rows[i], 0, i * TileHeight, TileWidth * AtlasWidth, TileHeight);
        }
    }

    public int GetRowIndex(BlockID block)
    {
        return indices[block];
    }

    public Rectangle GetTileBounds(int x, int y)
    {
        return new Rectangle(new(x / 6f, y / (float)rowCount), new(1 / 6f, 1f / rowCount));
    }

    public Rectangle GetTileBounds(BlockID block, Orientation side)
    {
        return GetTileBounds((int)side, indices[block]);
    }
}
