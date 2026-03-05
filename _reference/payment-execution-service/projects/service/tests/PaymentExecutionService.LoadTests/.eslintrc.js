const config = {
  env: {
    node: true,
  },

  extends: [
    require.resolve("eslint-config-airbnb-base"),
    "plugin:@typescript-eslint/eslint-recommended",
    "plugin:@typescript-eslint/recommended",
    "plugin:import/typescript",
    require.resolve("eslint-config-prettier"),
  ],

  overrides: [],

  parser: "@typescript-eslint/parser",
  parserOptions: {
    project: "tsconfig.json",
  },

  plugins: ["@typescript-eslint", "import", "prefer-arrow"],

  rules: {
    "@typescript-eslint/ban-ts-comment": "off",
    "@typescript-eslint/ban-ts-ignore": "off",
    "@typescript-eslint/explicit-function-return-type": "off",
    "@typescript-eslint/explicit-module-boundary-types": "off",
    "@typescript-eslint/member-ordering": "error",
    "@typescript-eslint/naming-convention": [
      "error",
      {
        format: ["camelCase"],
        leadingUnderscore: "allow",
        selector: "default",
        trailingUnderscore: "allow",
      },
      {
        format: null,
        selector: "objectLiteralProperty",
      },
      {
        format: null,
        selector: "objectLiteralMethod",
      },
      {
        format: ["camelCase", "PascalCase", "UPPER_CASE"],
        leadingUnderscore: "forbid",
        selector: "memberLike",
        trailingUnderscore: "forbid",
      },
      {
        format: ["camelCase", "PascalCase", "UPPER_CASE"],
        leadingUnderscore: "allow",
        selector: "variableLike",
        trailingUnderscore: "allow",
      },
      {
        format: ["PascalCase"],
        selector: "typeLike",
      },
      {
        format: ["PascalCase", "UPPER_CASE"],
        selector: "enum",
      },
    ],
    "@typescript-eslint/no-explicit-any": "off",
    "@typescript-eslint/no-inferrable-types": "off",
    "@typescript-eslint/no-non-null-assertion": "off",
    "@typescript-eslint/no-shadow": ["error"],
    "@typescript-eslint/no-unused-vars": ["error", { argsIgnorePattern: "^_" }],
    "comma-dangle": "off",
    "consistent-return": "off",
    "default-case": "off",
    "func-names": "off",
    "implicit-arrow-linebreak": "off",
    "import/extensions": "off",
    "import/no-unresolved": "off", // k6 is a Go package, not a JavaScript module, so it can't be resolved.
    "import/order": [
      "error",
      {
        alphabetize: {
          caseInsensitive: true,
          order: "asc",
        },
        groups: [
          "builtin",
          "external",
          "internal",
          ["parent", "sibling", "index"],
        ],
        "newlines-between": "always",
        pathGroups: [
          {
            group: "external",
            pattern: "@xero/**",
            position: "after",
          },
          {
            group: "internal",
            pattern: "~*",
          },
          {
            group: "internal",
            pattern: "~**/*",
          },
        ],
        pathGroupsExcludedImportTypes: ["builtin"],
      },
    ],
    "import/prefer-default-export": "off",
    "lines-between-class-members": [
      "error",
      "always",
      { exceptAfterSingleLine: true },
    ],
    "no-nested-ternary": "off",
    "no-param-reassign": [
      "error",
      {
        props: false,
      },
    ],
    "no-restricted-syntax": ["error", "LabeledStatement", "WithStatement"],
    "no-shadow": "off",
    "no-underscore-dangle": "off",
    "prefer-arrow/prefer-arrow-functions": [
      "error",
      {
        classPropertiesAllowed: false,
        disallowPrototype: true,
        singleReturnOnly: false,
      },
    ],
    "spaced-comment": "off",
  },
};

module.exports = config;
