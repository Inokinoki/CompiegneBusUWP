using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompiegneBus
{
    class Line
    {
        public static readonly string[] NAME = {
              "Gare -> Hôpital", "Hôpital -> Gare",
              "Port à Carreaux -> Gare -> C.C.Jaux Venette", "C.C. Jaux Venette -> Gare -> Port à Carreaux",
              "Marronniers -> Gare -> Ferdinand de Lesseps -> Hôpital", "Hôpital -> Ferdinand de Lesseps -> Gare -> Marronniers",
              "Palais -> Gare -> C.C. Jaux Venette", "C.C. Jaux Venette -> Gare -> Palais",
              "Gare -> Hôpital", "Hôpital -> Gare"
        };
    }
}
