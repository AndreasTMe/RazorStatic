using System;
using System.Collections.Generic;

namespace RazorStatic.Abstractions;

public interface IDirectoriesSetupForStaticContent : IEnumerable<ValueTuple<string, string[], string>>;