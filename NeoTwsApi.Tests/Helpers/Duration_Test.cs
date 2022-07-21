// created: 2022/06/23 9:47
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:

using System;
using FluentAssertions;
using FluentAssertions.Extensions;
using NeoTwsApi.Enums;
using NUnit.Framework;
using NeoTwsApi.Helpers;

namespace NeoTwsApi.Tests.Helpers;

public class Duration_Test
{
    [Test]
    public void ToDurationTws_Test()
    {
        {
            var ret = DurationTwsEx.ToDurationTws(DateTime.Parse("2022/07/01"), DateTime.Parse("2022/07/08"), ETimeFrameTws.D1);
            ret.Step.Should().Be(EDurationStep.D);
            ret.Quantity.Should().Be(7);
        }
        {
            var ret = DurationTwsEx.ToDurationTws(DateTime.Parse("2022/07/01"), DateTime.Parse("2022/07/08"), ETimeFrameTws.H1);
            ret.Step.Should().Be(EDurationStep.D);
            ret.Quantity.Should().Be(7);
        }
        {
            var ret = DurationTwsEx.ToDurationTws(DateTime.Parse("2022/07/01"), DateTime.Parse("2022/07/08"), ETimeFrameTws.M1);
            ret.Step.Should().Be(EDurationStep.M);
            ret.Quantity.Should().Be(10080);
        }




    }


}