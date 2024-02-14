using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchUtils.Checkers;

public interface ITwitchChecker
{
    public event EventHandler<TwitchCheckInfo>? ChannelChecked;
}
