﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Core.Dependency
{
    public class ServiceRegistrationActionList : List<Action<IOnServiceRegistredContext>>
    {

    }
}
