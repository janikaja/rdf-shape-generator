<%@ Page Title="ShExC schema creator" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeFile="About.aspx.cs" Inherits="About" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h1>About</h1>
    <p class="about">This tool accepts RDF data (Turtle, N-Triples, RDF/XML and RDF/JSON serializations) as an input and automatically generates a ShExC schema that can be used for validation.</p>
    
    <h2>Instructions</h2>
    <ol>
        <li>You have two initial options: a) enter manually prepared RDF data; b) select sample data from the dropdown.</li>
        <li>After you have entered or selected correctly formatted RDF data, you can get an automatically generated ShExC schema by pressing button &quot;Create ShExC schema&quot;.</li>
        <li>If your RDF data or sample data contains several instances (subjects) of different types (determined by property &quot;rdf:type&quot; or &quot;a&quot;), for the purpose of creating ShExC schema you will be asked to choose only one of them.</li>
        <li>If your RDF data or sample data contains several instances (subjects) of the same type but with different properties and their values, the created ShExC schema will conform to all given cases.</li>
        <li>This tool allows some fine-tuning or adjustment of generated constraints as well - depending on the entered RDF data, you can change cardinalities, create value sets or define ranges for numeric values. At the moment it is possible to define custom constraints that do not match the entered RDF data, but may match the data that will be validated afterwards.</li>
        <li>Additionally, if you have used a Turtle or N-Triples serialization for your data, you can test the generated ShExC schema in a validator that has been created by other authors: <a href="https://github.com/shexSpec/ShExC.js" target="_blank">ShExC.js</a>. This validator was chosen for being one of the most recent implementations of ShEx language.</li>
    </ol>
    
</asp:Content>
