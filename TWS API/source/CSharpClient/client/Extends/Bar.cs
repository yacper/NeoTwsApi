// created: 2022/06/29 14:49
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:Extend Tws Bar
// modifiers:

namespace IBApi
{
public partial class Bar
{
    public override string ToString()
    {
        return string.Format($" {Time} O:{Open} H:{High} L:{Low} C:{Close} V:{Volume} Wap:{WAP}");
    }
}
}