using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31;
internal class Block
{
    public bool Transparent { get; private set; }

    public bool Orientable { get; private set; }
}

struct BlockFaces
{
    public BlockFaces(string face)
    {
        Top = Bottom = Forward = Right = Backward = Left = face;
    }

    public string Top { get; set; }
    public string Bottom { get; set; }
    public string Forward { get; set; }
    public string Right { get; set; }
    public string Backward { get; set; }
    public string Left { get; set; }
}

