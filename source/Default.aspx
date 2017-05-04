﻿<%@ Page Title="ShEx shape creator" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" validateRequest="false" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1><%: Page.Title %></h1>
        <p class="lead">Welcome to the ShEx shape creator tool!</p>
    </div>

    <p class="prompt">Please, enter RDF data manually or select sample data:</p>
    <asp:DropDownList CssClass="form-control" ID="SampleList" OnSelectedIndexChanged="SampleList_SelectedIndexChanged" AutoPostBack="true" runat="server"></asp:DropDownList>

    <div class="row">
        <div class="col-md-12">
            <h2>RDF data:</h2>
            <asp:TextBox ID="source" TextMode="multiline" Columns="50" Rows="10" runat="server"></asp:TextBox>
        </div>
    </div>
    <% if (foundProperties.Count > 0) { %>
        <div class="row">
            <div class="col-md-12">
                <h2>Fine-tune constraints for the found properties of subject <%= targetSubject %>:</h2>
                <table class="foundProperties">
                  <tr>
                      <th>Property and the proposed constraint *</th>
                      <th>Change cardinality</th>
                      <th>Restrict value instead of type</th>
                      <th>Restrict range of numerical value</th>
                  </tr>
                  <% foreach (Property item in foundProperties) { %>
                    <tr>
                        <td><%= item.Name %></td>
                        <td>
                            <select id="Select<%= item.Index %>" name="Cardinality<%= item.Index %>">
                                <option value="1">Exactly 1 (default)</option>
                                <option value="2">0 or 1</option>
                                <option value="3">>= 0</option>
                                <option value="4">>= 1</option>
                            </select>
                            <label>
                                Min
                                <input type="number" name="min<%= item.Index %>" min="0" />
                            </label>
                            <label>
                                Max
                                <input type="number" name="max<%= item.Index %>" min="1" />
                            </label>
                        </td>
                        <td class="checkboxes">
                            <input id="Checkbox<%= item.Index %>" type="checkbox" name="getValueSet[]" value="<%= item.Name %>" <% if (item.Is_checked) { %> checked <% } %> />
                        </td>
                        <td>
                            <select id="ValueRangeMin<%= item.Index %>" name="ValueRangeMin<%= item.Index %>">
                                <option value="1">Min inclusive</option>
                                <option value="2">Min exclusive</option>
                            </select>
                            <input type="number" name="MinValue<%= item.Index %>" step="0.01" />
                            <select id="ValueRangeMax<%= item.Index %>" name="ValueRangeMax<%= item.Index %>">
                                <option value="1">Max inclusive</option>
                                <option value="2">Max exclusive</option>
                            </select>
                            <input type="number" name="MaxValue<%= item.Index %>" step="0.01" />
                        </td>
                    </tr>
                  <% } %>
                </table>
                <p>* according to the available RDF data</p>
            </div>
        </div>
    <% } %>

    <% if (shapeReady) { %>
        <asp:Button ID="submit" runat="server" Text="Update data shape" />
        <div class="row">
            <div class="col-md-12">
                <h2>ShEx data shape:</h2>
                <asp:TextBox ID="resultText" TextMode="multiline" Columns="50" Rows="10" runat="server"></asp:TextBox>
            </div>
        </div>
    <% } else { %>
        <asp:Button ID="Button1" runat="server" Text="Create data shape" />
    <% } %>

    <% if (additionalInfoRequired) { %>
        <p class="optionDescription">Please, select the class instance for which to create the data shape:</p>
        <asp:RadioButtonList ID="nodeOptions" runat="server">
        </asp:RadioButtonList>
    <% } %>
    <asp:HiddenField ID="infoRequired" Value="0" runat="server" />

    <p class="errorMessage"><%= error %></p>
</asp:Content>
