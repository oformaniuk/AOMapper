using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("AOMapper")]
[assembly: AssemblyDescription("Object-to-object convention-based mapping tool")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("AOMapper")]
[assembly: AssemblyCopyright("Copyright ©  2015 Oleh Formaniuk")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly:
    InternalsVisibleTo("AOMapperTests, PublicKey=0024000004800000940000000" +
                       "602000000240000525341310004000001000100ff828f" +
                       "d39595ecb1a9f7497d6878b4f1d82bf275b5d0ae4c59a" +
                       "4b3df3bd995dd90a2b4bcf85ea138d02f387a6056f356" +
                       "edf2d6d70b5363e2ac3a1cf5db627a3d7e533dec577a2" +
                       "a9f79c14c976fb083eac2dae6458cc5e35c0107a680bd" +
                       "5b3396a769cdd0b42520ed6f8da2e7f15e8d205cf8c55" +
                       "d2a1c268e7f5ca58ecb645ec1")]

[assembly:
    InternalsVisibleTo("ProfilerTarget, PublicKey=0024000004800000940000000" +
                       "602000000240000525341310004000001000100ff828f" +
                       "d39595ecb1a9f7497d6878b4f1d82bf275b5d0ae4c59a" +
                       "4b3df3bd995dd90a2b4bcf85ea138d02f387a6056f356" +
                       "edf2d6d70b5363e2ac3a1cf5db627a3d7e533dec577a2" +
                       "a9f79c14c976fb083eac2dae6458cc5e35c0107a680bd" +
                       "5b3396a769cdd0b42520ed6f8da2e7f15e8d205cf8c55" +
                       "d2a1c268e7f5ca58ecb645ec1")]

#if !PORTABLE
// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]


// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("05762837-1393-49f6-a3c0-5e2f13339718")]
[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityTransparent]
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
#endif