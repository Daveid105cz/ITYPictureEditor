using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ITYObraski
{
    public class MyLine:MyDrawable
    {
        public int Length { get; set; }
        public Vector Direction { get; set; }
        public float Thickness { get; set; }
    }
}
