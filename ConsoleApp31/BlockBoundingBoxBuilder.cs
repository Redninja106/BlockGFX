using System.Collections.Generic;

internal class BlockBoundingBoxBuilder
{
    public BlockBoundingBoxBuilder()
    {
    }

    public List<Box> Build(BlockChunk chunk, int width, int height, int depth)
    {
        List<Box> result = new();

        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!chunk[z * width * height + y * width + x].IsTransparent)
                    {
                        result.Add(new(new(x, y, z), new(x + 1, y + 1, z + 1)));
                    }
                }
            }
        }

        return result;
    }
}