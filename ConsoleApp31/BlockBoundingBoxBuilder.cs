using System.Collections.Generic;

internal class BlockBoundingBoxBuilder
{
    public BlockBoundingBoxBuilder()
    {
    }

    public List<Box> Build(BlockData[] blocks, int width, int height, int depth)
    {
        List<Box> result = new();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (!blocks[y * width * depth + x * depth + z].IsTransparent)
                    {
                        result.Add(new(new(x, y, z), new(x + 1, y + 1, z + 1)));
                    }
                }
            }
        }

        return result;
    }
}