﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Sdl.Community.NumberVerifier.Parsers.Number.Model;

namespace Sdl.Community.NumberVerifier.Parsers.Number
{
	public class NumberParser
	{
		private readonly List<NumberSeparator> _separators;
		private readonly List<string> _currencySymbols;

		public NumberParser(List<NumberSeparator> separators = null)
		{
			_separators = separators ?? DefaultNumberSeparators();

			CultureInfo.GetCultures(CultureTypes.AllCultures);
			_currencySymbols =
				CultureInfo.GetCultures(CultureTypes.AllCultures)
					.Select(culture => culture.NumberFormat.CurrencySymbol)
					.Distinct().ToList();
		}


		/// <summary>
		/// Parse number value from <param name="text"/>
		/// Convention: [ws] [$] [sign][integral-digits,]integral-digits[.[fractional-digits]][E[sign]exponential-digits][ws]
		/// </summary>
		/// <param name="text"></param>
		/// <returns>Returns a <see cref="NumberToken"/></returns>
		public NumberToken Parse(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}

			// parse out the currency symbol
			// [ws] [$]
			var currencyParts = GetCurrencyPart(text);
			if (currencyParts != null)
			{
				var value = currencyParts.Aggregate(string.Empty, (current, numberPart) => current + numberPart.Value);
				text = text.Substring(value.Length);
			}

			// parse out the sign
			// [sign]
			var signParts = GetSignPart(text);
			if (signParts != null)
			{
				var value = signParts.Aggregate(string.Empty, (current, numberPart) => current + numberPart.Value);
				text = text.Substring(value.Length);
			}

			// parse out the exponents
			//[E[sign]exponential-digits]
			var exponentialPart = GetExponentialPart(text);
			if (exponentialPart != null)
			{
				text = text.Substring(0, text.Length - exponentialPart.Value.Length);
			}

			// [integral-digits,]integral-digits[.[fractional-digits]]
			var integralAndFractionalParts = GetIntegralAndFractionalParts(text);

			// combine the individual number parts
			var numberParts = CombineParts(currencyParts, signParts, integralAndFractionalParts, exponentialPart);
			var numberToken = new NumberToken(text, numberParts);

			return numberToken;
		}

		/// <summary>
		/// Get [integral-digits,]integral-digits[.[fractional-digits]] parts
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		private List<NumberPart> GetIntegralAndFractionalParts(string text)
		{
			var parts = new List<NumberPart>();
			foreach (var chr in text.ToCharArray())
			{
				var numberType = GetNumberType(chr);

				var previousPart = parts.LastOrDefault();
				if (previousPart != null && numberType == NumberPart.NumberType.Number && previousPart.Type == NumberPart.NumberType.Number)
				{
					previousPart.Value += chr;
				}
				else
				{
					var valuePart = new NumberPart
					{
						Value = chr.ToString(),
						Type = numberType
					};

					valuePart.Message = valuePart.Type == NumberPart.NumberType.Invalid
						? string.Format("Separator is not recognized: {0}", chr)
						: null;

					parts.Add(valuePart);
				}
			}

			AssignNumericSeparatorTypes(parts);

			return parts;
		}

		/// <summary>
		/// Get [ws] [$] part
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		private List<NumberPart> GetCurrencyPart(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}

			var numberParts = new List<NumberPart>();

			var prefixWhiteSpaces = GetPrefixWhiteSpaces(text);
			var inputText = text.Substring(prefixWhiteSpaces.Length);
			
			
			if (!string.IsNullOrEmpty(prefixWhiteSpaces))
			{
				var numberPart = new NumberPart
				{
					Value = prefixWhiteSpaces,
					Type = NumberPart.NumberType.WhiteSpace
				};

				numberParts.Add(numberPart);
			}

			foreach (var currencySymbol in _currencySymbols)
			{
				if (!inputText.StartsWith(currencySymbol, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				
				var numberPart = new NumberPart
				{
					Value = currencySymbol,
					Type = NumberPart.NumberType.Currency
				};

				numberParts.Add(numberPart);
			}

			return numberParts;
		}


		/// <summary>
		/// Get [sign] part
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		private static List<NumberPart> GetSignPart(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}

			var numberParts = new List<NumberPart>();

			var prefixWhiteSpaces = GetPrefixWhiteSpaces(text);
			var inputText = text.Substring(prefixWhiteSpaces.Length);

			if (!string.IsNullOrEmpty(prefixWhiteSpaces))
			{
				var numberPart = new NumberPart
				{
					Value = prefixWhiteSpaces,
					Type = NumberPart.NumberType.WhiteSpace
				};

				numberParts.Add(numberPart);
			}
			
			var signRegex = new Regex(@"^\s*[+-]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			var match = signRegex.Match(inputText);
			if (match.Success)
			{
				var numberPart = new NumberPart
				{
					Value = match.Value,
					Type = NumberPart.NumberType.Sign
				};

				numberParts.Add(numberPart);
			}

			return numberParts;
		}

		/// <summary>
		/// Get [E[sign]exponential-digits][ws] part
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		private static NumberPart GetExponentialPart(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}

			var exponentialRegex = new Regex(@"[Ee]+(\-|\+|)[0-9]+\s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			var match = exponentialRegex.Match(text);
			if (match.Success)
			{
				return new NumberPart
				{
					Value = text.Substring(match.Index),
					Type = NumberPart.NumberType.Exponent
				};
			}

			return null;
		}

		private static string GetPrefixWhiteSpaces(string text)
		{
			var prefixSpacesRegex = new Regex(@"^\s+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			var match = prefixSpacesRegex.Match(text);
			if (match.Success)
			{
				return match.Value;
			}

			return string.Empty;
		}

		private static List<NumberPart> CombineParts(IReadOnlyCollection<NumberPart> currencyParts, IReadOnlyCollection<NumberPart> signParts,
			IReadOnlyCollection<NumberPart> integralAndFractionalParts, NumberPart exponentialPart)
		{
			var numberParts = new List<NumberPart>();

			if (currencyParts != null && integralAndFractionalParts.Count > 0)
			{
				numberParts.AddRange(currencyParts);
			}

			if (signParts != null && integralAndFractionalParts.Count > 0)
			{
				numberParts.AddRange(signParts);
			}

			if (integralAndFractionalParts != null && integralAndFractionalParts.Count > 0)
			{
				numberParts.AddRange(integralAndFractionalParts);
			}

			if (exponentialPart != null)
			{
				numberParts.Add(exponentialPart);
			}

			return numberParts;
		}

		private NumberPart.NumberType GetNumberType(char chr)
		{
			return char.IsNumber(chr)
				? NumberPart.NumberType.Number
				: _separators.Exists(a => a.Value == chr.ToString())
					? NumberPart.NumberType.Separator
					: char.IsWhiteSpace(chr)
						? NumberPart.NumberType.WhiteSpace
						: NumberPart.NumberType.Invalid;
		}

		private NumberPart.NumberType GetSeparatorType(char chr)
		{
			var separator = _separators.FirstOrDefault(a => a.Value == chr.ToString());
			if (separator != null)
			{
				var success = Enum.TryParse<NumberPart.NumberType>(separator.Type.ToString(), false, out var value);
				return success ? value : NumberPart.NumberType.Invalid;
			}

			return NumberPart.NumberType.Invalid;
		}

		private  bool IsOfSeparatorType(string value, NumberSeparator.SeparatorType type)
		{
			return _separators.Where(a => a.Type == type).Any(numberSeparator => numberSeparator.Value == value);
		}

		/// <summary>
		/// Assign the number separator types
		/// </summary>
		/// <param name="numberParts"></param>
		private  void AssignNumericSeparatorTypes(IReadOnlyList<NumberPart> numberParts)
		{
			if (numberParts == null || numberParts.Count == 0)
			{
				return;
			}

			var useGroupSeparatorOnly = false;
			var previousSeparatorTokenIndex = -1;
			for (var i = numberParts.Count - 1; i >= 0; i--)
			{
				if (numberParts[i].Type != NumberPart.NumberType.Separator)
				{
					continue;
				}

				if (previousSeparatorTokenIndex == -1)
				{
					// start by assuming that the last separator is always a decimal separator.
					// It is possible that this is not true, but can only be verified as we iterate
					// further back in the stack.
					if (IsOfSeparatorType(numberParts[i].Value, NumberSeparator.SeparatorType.DecimalSeparator))
					{
						numberParts[i].Type = NumberPart.NumberType.DecimalSeparator;
					}
					else if (IsOfSeparatorType(numberParts[i].Value, NumberSeparator.SeparatorType.GroupSeparator))
					{
						numberParts[i].Type = NumberPart.NumberType.GroupSeparator;
					}
				}
				else
				{
					// get the previous separator token
					var previousSeparatorToken = numberParts[previousSeparatorTokenIndex];

					if (previousSeparatorToken.Type != NumberPart.NumberType.Invalid)
					{
						if (IsOfSeparatorType(numberParts[i].Value, NumberSeparator.SeparatorType.GroupSeparator))
						{
							if ((previousSeparatorToken.Value != numberParts[i].Value && !useGroupSeparatorOnly)
							    || (previousSeparatorToken.Value == numberParts[i].Value
							        && previousSeparatorToken.Type == NumberPart.NumberType.GroupSeparator))
							{
								// if the char values are different, then we can start assuming that
								// the thousand char is used.
								numberParts[i].Type = NumberPart.NumberType.GroupSeparator;
							}
							else if (previousSeparatorToken.Value == numberParts[i].Value
							         && previousSeparatorToken.Type == NumberPart.NumberType.DecimalSeparator)
							{
								// we can assume that the numbers are only using thousand separators from here onwards.
								// this means that we can automatically change the previous token type from decimal
								// separator to thousand separator.
								useGroupSeparatorOnly = true;
								numberParts[i].Type = NumberPart.NumberType.GroupSeparator;
								previousSeparatorToken.Type = NumberPart.NumberType.GroupSeparator;
							}
							else
							{
								// should be all thousand separators from here onwards; 
								// if this is not true, then set as invalid
								numberParts[i].Type = NumberPart.NumberType.Invalid;
								numberParts[i].Message = string.Format("Mixed group separators: {0}{1}",
									numberParts[i].Value, numberParts[previousSeparatorTokenIndex].Value);
							}
						}
						else
						{
							//invalid not a group separator
							numberParts[i].Type = NumberPart.NumberType.Invalid;
							numberParts[i].Message = string.Format("Invalid group separator: {0}", numberParts[i].Value);
						}
					}

					// check for connected separator chars
					if (numberParts[i].Type != NumberPart.NumberType.Invalid && previousSeparatorTokenIndex == (i + 1))
					{
						numberParts[i].Type = NumberPart.NumberType.Invalid;
						numberParts[i].Message = "Invalid separator location; separators cannot be grouped together";
					}

					// check for 3 digits exist between the thousand separators
					if (numberParts[i].Type == NumberPart.NumberType.GroupSeparator && previousSeparatorTokenIndex > -1)
					{
						if (numberParts[i + 1].Value.Length != 3)
						{
							numberParts[i].Type = NumberPart.NumberType.Invalid;

							var valueText = GetPartText(numberParts, i, previousSeparatorTokenIndex);
							numberParts[i].Message =
								string.Format("The group value is out of range: {0}", valueText);
						}
					}
				}

				previousSeparatorTokenIndex = i;
			}

			//The last char must qualify as a number
			var lastValue = numberParts[numberParts.Count - 1];
			if (lastValue.Type != NumberPart.NumberType.Invalid && lastValue.Type != NumberPart.NumberType.Number)
			{
				lastValue.Type = NumberPart.NumberType.Invalid;
				lastValue.Message = "The last char is not a number";
			}
		}

		private static string GetPartText(IReadOnlyList<NumberPart> numberParts, int a, int b)
		{
			var valueText = string.Empty;
			for (var i = a; i < b; i++)
			{
				valueText += numberParts[i].Value;
			}

			return valueText;
		}

		private static List<NumberSeparator> DefaultNumberSeparators()
		{
			var numberFormat = Thread.CurrentThread.CurrentCulture.NumberFormat;
			return new List<NumberSeparator>
			{
				new NumberSeparator {Type = NumberSeparator.SeparatorType.DecimalSeparator, Value = numberFormat.NumberDecimalSeparator},
				new NumberSeparator {Type = NumberSeparator.SeparatorType.GroupSeparator, Value = numberFormat.NumberGroupSeparator}
			};
		}
	}
}
