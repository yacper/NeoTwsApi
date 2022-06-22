// /********************************************************************
//     created: 2022/06/22 16:59
//     author:  rush
//     email:  yacper@gmail.com
// 
//     purpose:
//     modifiers:
// *********************************************************************/

using FluentAssertions;
using NeoTwsApi.Enums;
using NUnit.Framework;

namespace NeoTwsApi.Tests.Enums;

[TestFixture]
public class ETimeFrameTws_TestClass
{
    [Test]
    public void ETimeFrameTws_ToString_Test()
    {
        ETimeFrameTws.S1.ToTwsString().Should().Be("1 secs");
        ETimeFrameTws.S5.ToTwsString().Should().Be("5 secs");
        ETimeFrameTws.S30.ToTwsString().Should().Be("30 secs");

        ETimeFrameTws.M1.ToTwsString().Should().Be("1 min");
        ETimeFrameTws.M2.ToTwsString().Should().Be("2 mins");
        ETimeFrameTws.M30.ToTwsString().Should().Be("30 mins");

        ETimeFrameTws.H1.ToTwsString().Should().Be("1 hour");
        ETimeFrameTws.H2.ToTwsString().Should().Be("2 hours");
        ETimeFrameTws.H8.ToTwsString().Should().Be("8 hours");


        ETimeFrameTws.D1.ToTwsString().Should().Be("1 day");

        ETimeFrameTws.W1.ToTwsString().Should().Be("1 week");

        ETimeFrameTws.MN1.ToTwsString().Should().Be("1 month");
    }

    [Test]
    public void String_ToETimeFrameTws_Test()
    {
        "1 secs".ToETimeFrameTws().Should().Be(ETimeFrameTws.S1);
        "5 secs".ToETimeFrameTws().Should().Be(ETimeFrameTws.S5);
        "30 secs".ToETimeFrameTws().Should().Be(ETimeFrameTws.S30);

        "1 min".ToETimeFrameTws().Should().Be(ETimeFrameTws.M1);
        "5 mins".ToETimeFrameTws().Should().Be(ETimeFrameTws.M5);
        "30 mins".ToETimeFrameTws().Should().Be(ETimeFrameTws.M30);

        "1 hour".ToETimeFrameTws().Should().Be(ETimeFrameTws.H1);
        "2 hours".ToETimeFrameTws().Should().Be(ETimeFrameTws.H2);
        "8 hours".ToETimeFrameTws().Should().Be(ETimeFrameTws.H8);


        "1 day".ToETimeFrameTws().Should().Be(ETimeFrameTws.D1);

        "1 week".ToETimeFrameTws().Should().Be(ETimeFrameTws.W1);

        "1 month".ToETimeFrameTws().Should().Be(ETimeFrameTws.MN1);
    }




}