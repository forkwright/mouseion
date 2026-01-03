// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Mouseion.Core.Filtering;

public enum FilterOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    In,
    NotIn
}

public enum FilterLogic
{
    And,
    Or
}

public class FilterCondition
{
    public string Field { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; }
    public string Value { get; set; } = string.Empty;
}

public class FilterRequest
{
    public List<FilterCondition> Conditions { get; set; } = new();
    public FilterLogic Logic { get; set; } = FilterLogic.And;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
