using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for Sample
/// </summary>
public class Sample
{
    public Sample()
    {
        //
        // TODO: Add constructor logic here
        //
    }

    public static string getSample(int index)
    {
        switch(index)
        {
            case 1:
                return "@prefix ex: <http://ex.example/ns#> .\n@prefix bibo: <http://purl.org/ontology/bibo/> .\n@prefix dc: <http://purl.org/dc/elements/1.1/> .\n@prefix dct:  <http://purl.org/dc/terms/> .\n@prefix foaf: <http://xmlns.com/foaf/0.1/> .\n@prefix owl: <http://www.w3.org/2002/07/owl#> .\n@prefix schema: <http://schema.org/> .\n@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .\n@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .\n\n<http://runa.lnb.lv/61933/>\n\ta bibo:CreativeWork;\n\tdct:isPartOf <http://dom.lndb.lv/data/obj/59600/> ;\n\tdct:isPartOf <http://dom.lndb.lv/data/obj/59676/> ;\n\tex:someInteger 2 ;\n\tex:someDecimal 3.14 ;\n\tbibo:recipient <http://runa.lndb.lv/lnc04-000123642/> ;\n\tbibo:place \"Rīga (Latvija) || Latvija\";\n\tdct:source <http://dom.lndb.lv/data/obj/61933/> ;\n\towl:sameAs <http://dom.lndb.lv/data/obj/61933/> .\n";
            case 2:
                return "@prefix bibo: <http://purl.org/ontology/bibo/> .\n@prefix dc: <http://purl.org/dc/elements/1.1/> .\n@prefix dct:  <http://purl.org/dc/terms/> .\n@prefix foaf: <http://xmlns.com/foaf/0.1/> .\n@prefix owl: <http://www.w3.org/2002/07/owl#> .\n@prefix schema: <http://schema.org/> .\n@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .\n@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .\n\n<http://runa.lnb.lv/61928/>\n\ta bibo:Letter;\n\tdct:isPartOf <http://dom.lndb.lv/data/obj/59600/> ;\n\tdct:isPartOf <http://dom.lndb.lv/data/obj/59676/> ;\n\tdct:creator <http://runa.lnb.lv/61928/> ;\n\tbibo:place \"Rīga (Latvija) || Latvija\";\n\tdct:date \"1894-05-30\";\n\tdct:source <http://dom.lndb.lv/data/obj/61928/> ;\n\towl:sameAs <http://dom.lndb.lv/data/obj/61928/> .\n";
            case 3:
                return "@prefix bibo: <http://purl.org/ontology/bibo/> .\n@prefix dc: <http://purl.org/dc/elements/1.1/> .\n@prefix dct:  <http://purl.org/dc/terms/> .\n@prefix foaf: <http://xmlns.com/foaf/0.1/> .\n@prefix owl: <http://www.w3.org/2002/07/owl#> .\n@prefix schema: <http://schema.org/> .\n@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .\n@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .\n\n<http://runa.lnb.lv/60969/>\n\ta bibo:Book ;\n\tdct:title \"Vaidelote : drama iz leišu pagātnes 5 cēlienos\"@lv ;\n\tdct:creator <http://runa.lnb.lv/60969/> ;\n\tdct:date \"1894\" ;\n\tdct:source <http://dom.lndb.lv/data/obj/60969/> ;\n\tdc:publisher \"Jelgava : H.J. Draviņ-Dravnieks\" ;\n\towl:sameAs <http://dom.lndb.lv/data/obj/60969/> .";
            default:
                return "";
        }
    }
}