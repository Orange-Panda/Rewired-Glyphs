using JetBrains.Annotations;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// Enum value used to communicate the result of glyph set queries.
	/// </summary>
	[PublicAPI]
	public enum GlyphSetQueryResult
	{
		/// <summary>
		/// Default value, methods should never return this value unless some unhandled exception occurs.
		/// </summary>
		/// <remarks>
		/// The value of the output list is undetermined if this value is somehow output.
		/// <br/><br/>
		/// If you get this result consider using the <see cref="InputGlyphs.UninitializedGlyph"/>
		/// </remarks>
		Undefined = 0,
		/// <summary>
		/// Represents a successful set query, no errors or invalid configuration occurred.
		/// </summary>
		/// <remarks>
		/// This does not necessarily mean there are any mappings/glyphs, just that no error occurred.
		/// <br/><br/>
		/// In other words, this means there are zero to many output items but those items accurately represents the query for the action.
		/// <br/><br/>
		/// If you get this result <b>and</b> the output list is empty consider using the <see cref="InputGlyphs.UnboundGlyph"/>
		/// </remarks>
		Success = 1,
		/// <summary>
		/// Represents a query that failed due to an unknown action identifier.
		/// <br/><br/>
		/// If you get this result validate the input action ID or name is correct.
		/// </summary>
		/// <remarks>
		/// The output list will be empty as a result since there is no action to represent.
		/// <br/><br/>
		/// If you get this result consider using the <see cref="InputGlyphs.NullGlyph"/>
		/// </remarks>
		ErrorUnknownAction = 2,
		/// <summary>
		/// Represents a query that failed due to the glyph system not being ready yet.
		/// </summary>
		/// <remarks>
		/// The output list will be empty since it is impossible for use to find actions or maps for those actions.
		/// <br/><br/>
		/// If you get this result consider using the <see cref="InputGlyphs.UninitializedGlyph"/>
		/// </remarks>
		ErrorUninitialized = 3
	}
}