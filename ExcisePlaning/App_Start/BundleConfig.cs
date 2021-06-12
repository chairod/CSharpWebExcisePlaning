using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;

namespace ExcisePlaning.App_Start
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            // -- jQuery bundle
            bundles.Add(new ScriptBundle("~/bundle/jquery").Include(
               "~/Third_Party/bower_components/jquery/js/jquery.min.js"));
            // Bootstrap bunder
            bundles.Add(new ScriptBundle("~/bundle/bootstrap").Include(
                "~/Third_Party/bower_components/bootstrap/bootstrap.min.js"));

            // -- Angularjs bundle
            ScriptBundle bundle = new ScriptBundle("~/bundle/angular");
            bundle.Include(
               "~/Third_Party/bower_components/angularjs/angular.min.js",
               "~/Third_Party/bower_components/angularjs/angular-animate.min.js",
               "~/Third_Party/bower_components/angularjs/angular-aria.min.js",
               "~/Third_Party/bower_components/angularjs/angular-cookies.min.js",
               "~/Third_Party/bower_components/angularjs/angular-sanitize.min.js",
               "~/Third_Party/bower_components/angular-material/angular-material.min.js", 
               "~/Third_Party/assets/js/angular.app.js");
            bundle.Orderer = new Classes.NonOrderingBundleOrderer();
            bundles.Add(bundle);


            // -- Required js bundle
            bundle = new ScriptBundle("~/bundle/required");
            bundle.Include(
               "~/Third_Party/bower_components/jquery-ui/jquery-ui.min.js",
               "~/Third_Party/bower_components/popper.js/js/popper.min.js",
               "~/Third_Party/bower_components/bootstrap/bootstrap.min.js",
               "~/Third_Party/bower_components/jquery-slimscroll/js/jquery.slimscroll.js",
               "~/Third_Party/bower_components/modernizr/js/modernizr.js",
               "~/Third_Party/bower_components/modernizr/js/css-scrollbars.js",
               "~/Third_Party/bower_components/classie/js/classie.js",
               "~/Third_Party/assets/js/jquery.mCustomScrollbar.concat.min.js",
               "~/Third_Party/assets/js/jquery.mousewheel.min.js",
               "~/Third_Party/assets/js/pcoded.min.js");
            bundle.Orderer = new Classes.NonOrderingBundleOrderer();
            bundles.Add(bundle);

            // bootstrap table fixed column
            bundle = new ScriptBundle("~/bundle/bootstrap-table-fixed");
            bundle.Include(
               "~/Third_Party/bower_components/bootstrap/bootstrap-table.min.js",
               "~/Third_Party/bower_components/bootstrap/bootstrap.min.js");
            bundle.Orderer = new Classes.NonOrderingBundleOrderer();
            bundles.Add(bundle);

            // -- Custom js bundle
            bundle = new ScriptBundle("~/bundle/custom");
            bundle.Include(
                "~/Third_Party/assets/js/script.js",
                 "~/Third_Party/assets/js/demo-12.js");
            bundle.Orderer = new Classes.NonOrderingBundleOrderer();
            bundles.Add(bundle);



            // -- Input mask bundle
            bundle = new ScriptBundle("~/bundle/inputmask");
            bundle.Include(
                "~/Third_Party/assets/pages/form-masking/inputmask.js",
                "~/Third_Party/assets/pages/form-masking/jquery.inputmask.js");
            bundle.Orderer = new Classes.NonOrderingBundleOrderer();
            bundles.Add(bundle);

            // -- Editor (Tinymce)
            bundle = new ScriptBundle("~/bundle/edtior");
            bundle.Include(
                "~/Third_Party/bower_components/wysiwyg-editor/js/tinymce.min.js");
            bundle.Orderer = new Classes.NonOrderingBundleOrderer();
            bundles.Add(bundle);

            // -- Input number bundle
            bundles.Add(new ScriptBundle("~/bundle/inputnumber").Include(
                "~/Third_Party/assets/pages/form-masking/autoNumeric.js"));
            
            // -- Select 2 bundle
            bundles.Add(new ScriptBundle("~/bundle/select2").Include(
                "~/Third_Party/bower_components/select2/js/select2.full.min.js"));
            bundles.Add(new StyleBundle("~/content/select2").Include(
                "~/Third_Party/bower_components/select2/css/select2.min.css"));

            // Swiper slider
            bundles.Add(new ScriptBundle("~/bundle/swipper").Include(
                "~/Third_Party/bower_components/swiper-slider/swiper.min.js"));
            bundles.Add(new StyleBundle("~/content/swipper").Include(
                "~/Third_Party/bower_components/swiper-slider/swiper.min.css"));

            // TimePicker
            bundles.Add(new ScriptBundle("~/bundle/timepicker").Include(
                "~/Third_Party/bower_components/Timepicki/timepicki.js"));
            bundles.Add(new StyleBundle("~/content/timepicker").Include(
                "~/Third_Party/bower_components/Timepicki/timepicki.css"));

            // Tags Input
            // ตัวอย่าง: http://localhost/Tags-Input-Autocomplete/example.html
            bundles.Add(new ScriptBundle("~/bundle/tags-input").Include(
                "~/Third_Party/bower_components/tags-input/jquery.tagsinput-revisited.js"));
            bundles.Add(new StyleBundle("~/content/tags-input").Include(
                "~/Third_Party/bower_components/tags-input/jquery.tagsinput-revisited.css"));

            // Image crop
            // https://cdnjs.com/libraries/croppie
            bundles.Add(new ScriptBundle("~/bundle/imageCrop").Include(
                "~/Third_Party/bower_components/imageCroppie/croppie.min.js"));
            bundles.Add(new StyleBundle("~/content/imageCrop").Include(
                "~/Third_Party/bower_components/imageCroppie/croppie.min.css"));

            // Datepicker range
            bundle = new ScriptBundle("~/bundle/datepickerrange");
            bundle.Include(
                "~/Third_Party/bower_components/datepickerrange/moment.min.js",
                "~/Third_Party/bower_components/datepickerrange/daterangepicker.js");
            bundle.Orderer = new Classes.NonOrderingBundleOrderer();
            bundles.Add(bundle);
            bundles.Add(new StyleBundle("~/content/datepickerrange").Include(
                "~/Third_Party/bower_components/datepickerrange/daterangepicker.css"));


            // Fusion chart
            // https://www.fusioncharts.com/;
            bundle = new ScriptBundle("~/bundle/fusionChart");
            bundle.Include(
                "~/Third_Party/bower_components/fusioncharts/scripts/fusioncharts.js",
                "~/Third_Party/bower_components/fusioncharts/scripts/integrations/jquery/js/jquery-fusioncharts.js",
                "~/Third_Party/bower_components/fusioncharts/scripts/themes/fusioncharts.theme.fusion.js",
                "~/Third_Party/bower_components/fusioncharts/scripts/themes/fusioncharts.theme.candy.js",
                "~/Third_Party/bower_components/fusioncharts/scripts/themes/fusioncharts.theme.umber.js");
            bundle.Orderer = new Classes.NonOrderingBundleOrderer();
            bundles.Add(bundle);


            // -- Application style bundle
            bundles.Add(new StyleBundle("~/content/css").Include(
                "~/Third_Party/bower_components/bootstrap/bootstrap.min.css",
                "~/Third_Party/bower_components/jquery-ui/jquery-ui.min.css",
                "~/Third_Party/assets/css/jquery.mCustomScrollbar.css",
                "~/Third_Party/assets/css/style.css",
                "~/Third_Party/bower_components/angular-material/angular-material.min.css",
                "~/Third_Party/bower_components/animate.css/css/animate.css"));

            // bootstrap table fixed column
            bundles.Add(new StyleBundle("~/content/bootstrap-table-fixed").Include(
                "~/Third_Party/bower_components/bootstrap/bootstrap-table.min.css",
                "~/Third_Party/bower_components/bootstrap/bootstrap-table-fixed-columns.min.css"));

            // -- Font bundle
            bundles.Add(new StyleBundle("~/Contents/fonts").Include(
                "~/Third_Party/assets/icon/themify-icons/themify-icons.css",
                "~/Third_Party/assets/icon/icofont/icofont.min.css",
                "~/Third_Party/assets/icon/flag-icons/css/flag-icon.min.css",
                "~/Third_Party/assets/icon/font-awesome/css/font-awesome.min.css",
                "~/Third_Party/assets/css/linearicons.css",
                "~/Third_Party/assets/css/simple-line-icons.css",
                "~/Third_Party/assets/css/ionicons.css"));

            
        }
    }
}