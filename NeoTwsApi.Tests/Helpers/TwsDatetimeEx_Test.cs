// created: 2022/06/23 9:47
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:

using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using NeoTwsApi.Helpers;

namespace NeoTwsApi.Tests.Helpers;

public class TwsDatetimeEx_Test
{
    [Test]
    public void ToTwsDateTime_Test()
    {
        "20220317  00:00:00".ToTwsDateTime().Should().Be(17.March(2022));


    }

    [Test]
    public void ToTwsDateTimeString()
    {
        17.March(2022).ToTwsDateTimeString().Should().Be("20220317  00:00:00");


    }


}