using System.Text.RegularExpressions;

namespace LocalizationServiceIntegration
{
    public static class TemplateHelper
    {

        /// <summary>
        /// Костыль для фрейзаппа. Они не могут в наш синтаксис для параметров и зашкваривают перевод, вставляя туда
        /// пробел между $ и {, квокка такое надругательство не воспринимает и не считает за синтаксис шаблона вообще
        /// Поэтому меняем обратно, когда засасываем из фрейзаппа. :-(
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ProcessTemplateSyntax(this string value)
        {
            return Regex.Replace(value, @"\$\s\s*{", "${");
        }
    }
}