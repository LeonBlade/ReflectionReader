using Mono.Cecil;
using System;
using System.Linq;

namespace ReflectionReader
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("{ \"error\": \"Invalid number of arguments.\" }");
				return;
			}

			var module = ModuleDefinition.ReadModule(args[0]);
			var type = module.GetType(args[1]);

			ReadType(type.Resolve());

			Console.ReadKey();
		}

		static void ReadType(TypeDefinition type)
		{
			if (!type.BaseType.IsValueType && type.BaseType.Equals(typeof(object)))
				ReadType(type.BaseType.Resolve());

			var properties = type.Properties;
			var fields = type.Fields;

			foreach (var prop in properties)
			{
				if (IsValid(prop, out string error))
					Console.WriteLine($"Name: \"{prop.Name}\", Type: \"{prop.PropertyType}\", Is Value: {prop.PropertyType.IsValueType}");
				else
					Console.WriteLine("ERROR: " + error);
			}

			foreach (var field in fields)
			{
				if (IsValid(field, out string error))
					Console.WriteLine($"Name: \"{field.Name}\", Type: \"{field.FieldType}\", Is Value: {field.FieldType.IsValueType}");
				else
					Console.WriteLine("ERROR: " + error);
			}
		}

		static bool IsStaticType(TypeDefinition type) => type.IsAbstract && type.IsSealed;

		/// <summary>
		/// Ensures that a field or property is valid for deserialization.
		/// </summary>
		/// <param name="m">The field or property to be validated.</param>
		/// <returns>Boolean value if it's valid for serialization.</returns>
		static bool IsValid(IMemberDefinition m, out string error)
		{
			// Get as field and property.
			var field = m as FieldDefinition;
			var property = m as PropertyDefinition;

			// Cannot be a static member.
			if (IsStaticType(m.DeclaringType))
			{
				error = "INVALID: Cannot be static!";
				return false;
			}

			// Cannot be decorated with ContentSerializerIgnore attribute.
			if (m.CustomAttributes.Where(a => a.AttributeType.FullName == "Microsoft.Xna.Framework.Content.ContentSerializerIgnoreAttribute").Count() != 0)
			{
				error = "INVALID: ContentSerializerIgnore decorated";
				return false;
			}

			// MemberInfo is a property.
			if (property != null)
			{
				// Property getter isn't public.
				if (!property.GetMethod.IsPublic)
				{
					error = "INVALID: Getter isn't public!";
					return false;
				}

				// Index property.
				if (property.HasParameters)
				{
					error = "INVALID: Is an index property!";
					return false;
				}
			}

			// Isn't decorated with ContentSerializer
			if (m.CustomAttributes.Where(a => a.AttributeType.FullName == "Microsoft.Xna.Framework.Content.ContentSerializerAttribute").Count() == 0)
			{
				// MemberInfo is a property.
				if (property != null)
				{
					// Property getter isn't public (isn't gettable).
					if (!property.GetMethod.IsPublic)
					{
						error = "INVALID: Can't read getter!";
						return false;
					}

					// Property setter isn't public (isn't settable).
					if (!property.SetMethod.IsPublic)
					{
						error = "INVALID: Setter isn't public!";
						return false;
					}
				}
				// MemberInfo is a field.
				else
				{
					// Field isn't public.
					if (!field.IsPublic)
					{
						error = "INVALID: Field isn't public!";
						return false;
					}
				}
			}

			// No error :)
			error = null;
			return true;
		}
	}
}
