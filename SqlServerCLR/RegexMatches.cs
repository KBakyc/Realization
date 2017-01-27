using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Server;

internal class MatchNode
{
   private int _index;
   public int Index
   {
      get
      {
         return _index;
      }
   }
   private string _value;
   public string Value
   {
      get
      {
         return _value;
      }
   }
   public MatchNode( int index, string value )
   {
      _index = index;
      _value = value;
   }
}

internal class MatchIterator : IEnumerable
{
   private Regex _regex;
   private string _input;

   public MatchIterator( string input, string pattern )
   {
      _regex = new Regex( pattern, UserDefinedFunctions.Options );
      _input = input;
   }

   public IEnumerator GetEnumerator( )
   {
      int index = 0;
      Match current = null;
      while(current == null || current.Success)
      {
         current = (current == null) ?
             _regex.Match( _input ) : current.NextMatch( );
         if(current.Success)
         {
            yield return new MatchNode( ++index, current.Value );
         }
      }
   }
}

public static partial class UserDefinedFunctions
{
   [SqlFunction( FillRowMethodName = "FillMatchRow",
       TableDefinition = "[Index] int,[Text] nvarchar(max)" )]
   public static IEnumerable
       RegexMatches( SqlChars input, SqlString pattern )
   {
      return new MatchIterator( new string( input.Value ), pattern.Value );
   }

   [SuppressMessage( "Microsoft.Design", "CA1021:AvoidOutParameters" )]
   public static void FillMatchRow( object data,
       out SqlInt32 index, out SqlChars text )
   {
      MatchNode node = (MatchNode)data;
      index = new SqlInt32( node.Index );
      text = new SqlChars( node.Value.ToCharArray( ) );
   }
}