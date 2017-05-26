<%@ Page Title="ShExC schema creator" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" validateRequest="false" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1><%: Page.Title %></h1>
        <p class="lead">Welcome to ShExC schema creator tool!</p>
    </div>

    <p class="about">This tool accepts RDF data (Turtle, N-Triples, RDF/XML and RDF/JSON serializations) as an input and automatically generates a ShExC schema that can be used for validation.</p>
    <p class="about">You can find more detailed instructions in <a href="About">&quot;About&quot;</a> section.</p>
    <p class="prompt">Please, enter RDF data manually or select sample data:</p>
    <asp:DropDownList CssClass="form-control" ID="SampleList" OnSelectedIndexChanged="SampleList_SelectedIndexChanged" AutoPostBack="true" runat="server"></asp:DropDownList>

    <div class="row">
        <div class="col-md-12">
            <h2>RDF data:</h2>
            <asp:TextBox ID="source" TextMode="multiline" Columns="50" Rows="10" runat="server"></asp:TextBox>
            <asp:Button ID="Button2" runat="server" Text="Update schema" />
        </div>
    </div>
    <% if (error.Length == 0 && foundProperties.Count > 0) { %>
        <div class="row">
            <div class="col-md-12">
                <h2 id="foundPropertiesHeading">Fine-tuning of constraints</h2>
                <input id="fineTune" type="button" value="Show" data-label1="Show" data-label2="Hide" />
                <div class="toggleContents">
                <table class="foundProperties">
                  <tr>
                      <th>Property and the proposed constraint *</th>
                      <th>Change cardinality</th>
                      <th>Create value set</th>
                      <% if (hasNumericValues) { %>
                        <th>Restrict range of numeric values</th>
                      <% } %>
                  </tr>
                  <% foreach (Property item in foundProperties) { %>
                    <tr>
                        <td><%= item.Name %></td>
                        <td>
                                <select id="Select<%= item.Index %>" name="cardinality<%= item.Index %>" class="cardinalities" data-index="<%= item.Index %>">
                                    <option value="0" <% if (item.Cardinality_index == 0) { %>selected<% } %>></option>
                                    <option value="1" <% if (item.Cardinality_index == 1) { %>selected<% } %>>Exactly 1 (default)</option>
                                    <option value="2" <% if (item.Cardinality_index == 2) { %>selected<% } %>>0 or 1 (?)</option>
                                    <option value="3" <% if (item.Cardinality_index == 3) { %>selected<% } %>>>= 0 (*)</option>
                                    <option value="4" <% if (item.Cardinality_index == 4) { %>selected<% } %>>>= 1 (+)</option>
                                </select>
                                <span class="separator">OR</span>
                                <label>
                                    Min
                                    <input type="number" name="min<%= item.Index %>" min="0" value="<%= item.Min %>" class="inputCardinalities" data-index="<%= item.Index %>" />
                                </label>
                                <label>
                                    Max
                                    <input type="number" name="max<%= item.Index %>" min="1" value="<%= item.Max %>" class="inputCardinalities" data-index="<%= item.Index %>" />
                                </label>
                        </td>
                        <td class="checkboxes">
                            <% if (!item.hasBlankNode()) { %>
                                <label>
                                    <input id="Checkbox<%= item.Index %>" type="checkbox" name="getValueSet[]" value="<%= item.Name %>" <% if (item.Is_checked) { %> checked <% } %> />
                                </label>
                            <% } %>
                        </td>
                        <% if (hasNumericValues) { %>
                            <td>
                                <% if (item.hasIntegerProperty() || item.hasDecimalProperty()) { %>
                                    <select id="ValueRangeMin<%= item.Index %>" name="valueRangeMin<%= item.Index %>">
                                        <option value="MinInclusive" <% if (!item.Range.Is_empty && item.Range.Min_type == "MinInclusive") { %>selected<% } %>>Min inclusive</option>
                                        <option value="MinExclusive" <% if (!item.Range.Is_empty && item.Range.Min_type == "MinExclusive") { %>selected<% } %>>Min exclusive</option>
                                    </select>
                                    <input type="number" name="minValue<%= item.Index %>" <% if (item.hasDecimalProperty()) { %>step="0.01"<% } %> <% if (!item.Range.Is_empty) { %>value="<%= item.Range.getStandardValueMin() %>"<% } %> />
                                    <select id="ValueRangeMax<%= item.Index %>" name="valueRangeMax<%= item.Index %>">
                                        <option value="MaxInclusive" <% if (!item.Range.Is_empty && item.Range.Max_type == "MaxInclusive") { %>selected<% } %>>Max inclusive</option>
                                        <option value="MaxExclusive" <% if (!item.Range.Is_empty && item.Range.Max_type == "MaxExclusive") { %>selected<% } %>>Max exclusive</option>
                                    </select>
                                    <input type="number" name="maxValue<%= item.Index %>" <% if (item.hasDecimalProperty()) { %>step="0.01"<% } %> <% if (!item.Range.Is_empty) { %>value="<%= item.Range.getStandardValueMax() %>"<% } %> />
                                <% } %>
                            </td>
                        <% } %>
                    </tr>
                  <% } %>
                </table>
                <p>* according to the given RDF data</p>
                <asp:Button ID="submit" runat="server" Text="Update schema" />
                </div>
            </div>
        </div>
    <% } %>

    <% if (shapeReady) { %>
        <div class="row">
            <div class="col-md-12">
                <h2>ShExC schema:</h2>
                <asp:TextBox ID="resultText" TextMode="multiline" Columns="50" Rows="10" ReadOnly="true" runat="server"></asp:TextBox>
            </div>
        </div>
        <% if (!hideValidatorLink) { %>
            <a href="http://rawgit.com/shexSpec/shex.js/master/doc/shex-simple.html?interface=simple&schema=<%= encodedSchema %>&data=<%= encodedData %>&shape-map=<%= targetSubject %>%40my%3ASchema" target="_blank">Test RDF data and ShExC schema in a validator (https://github.com/shexSpec/shex.js)</a>
        <% } %>
    <% } else if (!additionalInfoRequired) { %>
        <asp:Button ID="Button1" runat="server" Text="Create ShExC schema" />
    <% } %>

    <% if (additionalInfoRequired) { %>
        <p class="optionDescription">Please, select the instance for which to create schema:</p>
        <asp:RadioButtonList ID="nodeOptions" runat="server" AutoPostBack="true"></asp:RadioButtonList>
    <% } %>
    <asp:HiddenField ID="infoRequired" Value="0" runat="server" />

    <p class="errorMessage"><%= error %></p>
</asp:Content>
