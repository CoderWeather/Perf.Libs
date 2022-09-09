using System.Reflection;

#if NET5_0_OR_GREATER
[assembly: AssemblyMetadata("IsTrimmable", "True")]
#else
[assembly: AssemblyMetadata("IsTrimmable", "False")]
#endif