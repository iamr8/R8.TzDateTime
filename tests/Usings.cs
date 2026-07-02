global using FluentAssertions;
global using System.Collections.Concurrent;
global using System.Text.Json;
global using System.Globalization;
global using System.Diagnostics;
global using NodaTime;

#if NET8_0_OR_GREATER
global using Xunit;
#else
global using Xunit;
global using Xunit.Abstractions;
#endif