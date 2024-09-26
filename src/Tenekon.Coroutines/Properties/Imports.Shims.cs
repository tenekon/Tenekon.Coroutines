#if NETSTANDARD2_1
extern alias shim;
global using MethodImplOptions = shim.System.Runtime.CompilerServices.MethodImplOptions;
#endif
