#!/usr/bin/env python3
"""
Post-process OpenAPI spec to fix .NET 10 issues:
1. Integer/string union types (number | string -> number)
2. Enum types serialized as integers but actually sent as strings (JsonStringEnumConverter)
"""

import json
import sys

# Enums that the API serializes as strings via JsonStringEnumConverter.
# Map schema name -> list of string enum values.
STRING_ENUMS = {
    "DailyCommitmentStatus": ["Committed", "Completed", "Dismissed", "Missed"],
    "TaskItemStatus": ["Pending", "InProgress", "Completed", "Cancelled"],
    "ProofIntensity": ["Easy", "Moderate", "Hard"],
}


def fix_schema(obj):
    if isinstance(obj, dict):
        # Fix type arrays that include "string" alongside numeric types
        if "type" in obj and isinstance(obj["type"], list):
            types = obj["type"]
            # Remove "string" from type arrays that contain integer or number
            # (caused by .NET 10's large number string representation support)
            if ("integer" in types or "number" in types) and "string" in types:
                types.remove("string")
                obj.pop("pattern", None)
            # Simplify single-element arrays
            if len(types) == 1:
                obj["type"] = types[0]

        for value in obj.values():
            fix_schema(value)
    elif isinstance(obj, list):
        for item in obj:
            fix_schema(item)


def fix_enums(spec):
    schemas = spec.get("components", {}).get("schemas", {})
    for name, values in STRING_ENUMS.items():
        if name in schemas:
            schemas[name] = {
                "type": "string",
                "enum": values,
            }


def main():
    input_file = sys.argv[1] if len(sys.argv) > 1 else "openapi/v1.json"
    output_file = sys.argv[2] if len(sys.argv) > 2 else input_file

    with open(input_file) as f:
        spec = json.load(f)

    fix_schema(spec)
    fix_enums(spec)

    with open(output_file, "w") as f:
        json.dump(spec, f, indent=2)

    print(f"Fixed OpenAPI spec: {output_file}")


if __name__ == "__main__":
    main()
