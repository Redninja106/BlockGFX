using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31;
internal interface IGameComponent
{
    void Render(Camera camera);
    void Update(float dt);
}
