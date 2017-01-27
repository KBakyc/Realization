using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Server;

[SuppressMessage( "Microsoft.Design", "CA1050:DeclareTypesInNamespaces" )]
public static partial class UserDefinedFunctions
{
   [SqlFunction]
   public static SqlChars
        RegexGroup( SqlChars input, SqlString pattern, SqlString name )
   {
      Regex regex = new Regex( pattern.Value, Options );
      Match match = regex.Match( new string( input.Value ) );
      return match.Success ?
            new SqlChars( match.Groups[name.Value].Value ) : SqlChars.Null;
   }

}