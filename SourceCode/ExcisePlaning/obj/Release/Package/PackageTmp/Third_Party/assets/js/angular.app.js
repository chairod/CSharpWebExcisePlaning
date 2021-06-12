angular.module('leaveApp', ['ngMaterial', 'ngAnimate', 'ngCookies', 'ngSanitize'])
    .config(function ($httpProvider, $mdThemingProvider, $provide) {
        $httpProvider.defaults.withCredentials = true;
        $httpProvider.interceptors.push('customInterceptor');


    }).run(function ($rootScope) {
        // ภาษาที่ใช้ในระบบ
        $rootScope.defaultLocale = 'th-TH';
        //$rootScope.defaultLocale = 'en-US';

        // Base Url หลักของระบบ
        $rootScope.baseUrl = 'http://localhost:2194/';
        //var pageHeader = $('.page-header');
        //var pageBody = $('.page-body');
        //var navbarMenu = $('.pcoded-inner-navbar');
        //$(document).scroll(function (e) {
        //    // -- Freeze page header greater than or equal screen width 600
        //    if ($(window).width() >= 600) {
        //        if ($(this).scrollTop() > 100) {
        //            // var _elementWidth = $(window).width() - navbarMenu.width() +
        //            // 15;
        //            // var _left = navbarMenu.width() - 15;
        //            var _left = navbarMenu.width();
        //            var _elementWidth = ($(document).width() - navbarMenu.width()) + 'px';

        //            // -- Menu Left
        //            // if(navbarMenu.hasClass('main-menu')){
        //            // // -- Foreshorted menu
        //            // if(!navbarMenu.hasClass('mCS_no_scrollbar')){
        //            // _elementWidth = $(window).width() - 30;
        //            // _left = 30;
        //            // }
        //            // }


        //            pageHeader.addClass('fixed-page-header z-depth-bottom-3');
        //            pageHeader.animate({
        //                // width: _elementWidth + 'px',
        //                left: _left,
        //                'width': _elementWidth,
        //                'z-index': '10'
        //            }, 0, 'swing');
        //        } else {
        //            pageHeader.removeClass('fixed-page-header z-depth-bottom-3');
        //            pageHeader.attr('style', '');
        //        }
        //    }
        //});
        //$(window).resize(function () {
        //    $(document).trigger('scroll');
        //});
        // document.addEventListener('scroll', function (event) {
        // if (event.target.id === 'idOfUl') { // or any other filtering condition
        // console.log('scrolling', event.target);
        // }
        // console.log('scrolling', event.target);
        // }, true /*Capture event*/);
    }).factory('customInterceptor', function ($q, $timeout, $window) {
        return {
            responseError: function (rejection) {
                $timeout(function () {
                    if (undefined != rejection.data && undefined != rejection.data.message) {
                        alert("เกิดข้อผิดพลาด โปรดแจ้งผู้พัฒนาให้ตรวจสอบ Error Log ของระบบ\n\n\nERROR: => " + rejection.data.message);
                    } else if (undefined != rejection.message && '' != rejection.message) {
                        var errorText = rejection.message;
                        var regex = /.*(Unauthorize).*/ig;
                        if (regex.test(errorText))
                            $window.location.reload();
                    } else if (undefined != rejection.xhrStatus) {
                        if (401 == rejection.status) {
                            $window.location.reload();
                        } else if (500 == rejection.status) {
                            alert("เกิดข้อผิดพลาด โปรดแจ้งผู้พัฒนาให้ตรวจสอบ Error Log ของระบบ");
                        } else {
                            var errorText = "ไม่สามารถติดต่อกับ Server ได้โปรดลองอีกครั้งในภายหลัง\n\n[StatusCode: " + rejection.status + '] : ' + rejection.statusText;
                            alert(errorText);
                        }
                    }
                }, 100);
                return $q.reject(rejection);
            }
        };
    }).service('$customHttp', ['$http', function ($http) {
        this.formPost = function (url, params, token) {
            return $http.post(url, $.param(params), {
                headers: {
                    'X-Requested-With': 'XMLHttpRequest', // Tell Asp.net to know IsAjaxRequest
                    "Content-Type": "application/x-www-form-urlencoded",
                    "Authorization": 'Basic ' + token
                }
            });
        };

        this.formPut = function (url, params, token) {
            return $http.put(url, $.param(params), {
                headers: {
                    'X-Requested-With': 'XMLHttpRequest', // Tell Asp.net to know IsAjaxRequest
                    "Content-Type": "application/x-www-form-urlencoded",
                    "Authorization": 'Basic ' + token
                }
            });
        };

        this.formDelete = function (url, params, token) {
            return $http.delete(url + '?' + $.param(params), {
                headers: {
                    'X-Requested-With': 'XMLHttpRequest', // Tell Asp.net to know IsAjaxRequest
                    "Content-Type": "application/x-www-form-urlencoded",
                    "Authorization": 'Basic ' + token
                }
            });
        };


        this.formGet = function (url, params, token) {
            return $http.get(url + '?' + $.param(params), {
                headers: {
                    'X-Requested-With': 'XMLHttpRequest', // Tell Asp.net to know IsAjaxRequest
                    "Content-Type": "application/x-www-form-urlencoded",
                    "Authorization": 'Basic ' + token
                }
            });
        };
    }]).service('$fwUtils', function () {
        this.parseJson = function (jsonStr) {
            if (typeof jsonStr == 'string') {
                return ($.trim(jsonStr) == '' ? null : JSON.parse(jsonStr));
            }

            return jsonStr;
        };
        this.toJson = function (obj) {
            return angular.toJson(obj);
        };

        // Total days
        // date1, date2 are object Date()
        this.diffDateReturnDays = function (date1, date2) {
            var diffMilisec = Math.abs(date2 - date1);
            return Math.ceil(diffMilisec / (1000 * 60 * 60 * 24));
        };
    }).service('$fwDateService', function ($rootScope, $filter) {
        this.currDate = new Date();
        // Culture list => https://dotnetfiddle.net/e1BX7M
        this.locale = $rootScope.defaultLocale;

        // ระบุรูปแบบเวลาใหม่เพื่อใช้ format วันที่ให้อยู่ในรูปแบบข้อความ
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date/toLocaleDateString
        this.setLocale = function (newLocale) {
            this.locale = newLocale;
            return this;
        }

        this.defaultLocale = function () {
            this.locale = $rootScope.defaultLocale;
            return this;
        };

        // ร้องขอวัน/เวลาปัจจุบัน (จะได้ค่าเวลาใหม่ ณ เวลาที่่เรียกใช้งานฟังชันก์ นี้)
        this.getCurrDate = function () {
            this.currDate = new Date();
            return this;
        };

        // เปลี่ยนแปลงเวลาให้เป็นเดือนก่อนหน้า (นับจากเดือนปัจจุบัน)
        this.getPrevMonth = function () {
            this.currDate.setMonth(this.currDate.getMonth() - 1);
            return this;
        };

        // เปลี่ยนแปลงวัน (ส่งค่ามาเป็นจำนวนเต็ม หรือ ค่าติดลบก็ได้)
        this.addDays = function (addDays) {
            var dateVal = this.currDate.getDate() + (addDays) + 1;
            this.currDate.setDate(dateVal);
            return this;
        };

        // เปลี่ยนแปลงเดือน (ส่งค่ามาเป็นจำนวนเต็ม หรือ ค่าติดลบก็ได้)
        this.addMonths = function (addMonths) {
            var monthVal = this.currDate.getMonth() + (addMonths) + 1;
            this.currDate.setMonth(monthVal);
            return this;
        };

        // ค่าปีงบประมาณเริ่มต้น ของปัจจุบัน
        this.getBeginFiscalYear = function () {
            var d = new Date();
            var year = d.getFullYear();
            var month = d.getMonth(); // jan start 0
            if (month < 9) // เดือนตุลาคม
                year -= 1;
            this.currDate = new Date(year, 9, 1);
            return this;
        };

        // ค้นหา วัน/เดือน/ปี ที่สิ้นของของปีงบประมาณ
        this.getEndFiscalYear = function () {
            var d = new Date();
            var year = d.getFullYear();
            var month = d.getMonth();
            if (month >= 9)
                year += 1;
            this.currDate = new Date(year, 8, 30);
            return this;
        };

        // ค้นหา วัน/เดือน/ปี ที่สิ้นสุดไตรมาสแรก
        this.getEndFirstHalfQuater = function () {
            this.getBeginFiscalYear().addMonths(4);
            this.currDate.setDate(31);
            return this;
        };

        // ค้นหา วัน/เดือน/ปี ที่เริ่มต้นไตรมาสสอง
        this.getBeginHalfQuater = function () {
            this.getBeginFiscalYear().addMonths(5);
            this.currDate.setDate(1);
            return this;
        };

        // แปลงเวลาที่เป็น Javascript date object ให้อยู่ในรูปแบบข้อความ
        // โดยอ้างอิงตาม Locale
        // options = กรณีต้องการ Customize เองให้ผ่านค่าเข้ามา อ้างอิงจากเว็บ https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date/toLocaleDateString
        //  ไม่แสดงเวลา  => { year: 'numeric', month: '2-digit', day: '2-digit' }
        //  แสดงเวลา    => { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' };
        this.toString = function (options) {
            options = options || { year: 'numeric', month: '2-digit', day: '2-digit' };
            return this.currDate.toLocaleDateString(this.locale, options);
        }

        // แปลงเวลาที่เป็น Javascript date object ให้อยู่ในรูปแบบข้อความ
        // จะได้ผลลัพธ์เป็น เดือน/ปี (MM/YYYY)
        this.toShortString = function () {
            return this.toString({ year: 'numeric', month: '2-digit' });
        };

        // แปลงรูปแบบเวลา MM/yyyy (ปี พ.ศ.) ให้เป็นปี ค.ศ.
        this.convertShortStringBuddhistToChristian = function (strVal) {
            if (this.locale != 'th-TH' || strVal == '')
                return strVal;

            var parts = strVal.split('/').map(function (val) {
                return +val; // แปลงให้เป็นตัวเลข
            });
            return $filter('textFormat')('{0}/{1}', parts[0], parts[1] - 543);
        };

        // แปลงวันที่ที่อยู่ในรูปแบบ dd/MM/yyyy (พ.ศ.) ให้เป็น ค.ศ.
        this.parseDateStr = function (strVal) {
            var parts = strVal.split('/').map(function (val) {
                return +val; // แปลงให้เป็นตัวเลข
            });
            var year = parts[2];
            if (this.locale == 'th-TH')
                year -= 543; // แปลงให้เป็นปี ค.ศ.
            this.currDate = new Date(year, parts[1] - 1, parts[0], 0, 0, 0, 0);

            return this;
        };

        // diff ข้อมูลเวลา
        // ผลลัพธ์ x ปี x เดือน x วัน
        // dateStr อยู่ในรูปแบบ dd/MM/yyyy
        this.diffDate = function (dateStr) {
            var fromDate = this.parseDateStr(dateStr).toDate();
            var diffYears = moment().diff(fromDate, 'years');
            var diffMonths = moment().diff(fromDate, 'months') % 12;
            var diffDays = moment().diff(fromDate, 'days') % 30;

            var ageText = '';
            if (diffYears > 0)
                ageText = $filter('textFormat')('{0} ปี', diffYears);
            if (diffMonths > 0)
                ageText = $filter('textFormat')('{0} {1} เดือน', ageText, diffMonths);
            if (diffDays > 0)
                ageText = $filter('textFormat')('{0} {1} วัน', ageText, diffDays);

            return ageText;
        };

        // แปลงรูปแบบปี ค.ศ. ให้อยู่ในรูปแบบปี พ.ศ.
        // year ปี ค.ศ. ที่ต้องการแปลง
        this.convertYearToBuddhist = function (year) {
            var sourceYear = +year; // แปลงให้อยู่ในรูปแบบตัวเลข
            if (this.locale != 'th-TH' || sourceYear == 0)
                return year;

            return sourceYear + 543;
        };

        // แปลงรูปแบบปี พ.ศ. ให้อยู่ในรูปแบบปี ค.ศ.
        // year ปี พ.ศ. ที่ต้องการแปลง
        this.convertYearToBritish = function (year) {
            var sourceYear = +year; // แปลงให้อยู่ในรูปแบบตัวเลข
            if (this.locale != 'th-TH' || sourceYear == 0)
                return year;

            return sourceYear - 543;
        };

        // ต้องการเวลาที่เป็น Javascript date object
        this.toDate = function () {
            return this.currDate;
        };

    }).service('$fwWrapToastService', function ($mdToast) {
        this.alert = function (text) {
            return $mdToast.show(
                $mdToast.simple().textContent(text).position('top left').hideDelay(1500));
        };
    }).service('$fwModalService', function ($mdDialog, $timeout) {
        this.getModal = function (_templateUrl, _params, _controller, event) {
            return $mdDialog.show({
                parent: angular.element(document.body),
                clickOutsideToClose: false,
                targetEvent: event,
                openForm: (event ? event.target : null),
                skipHide: true,
                autoWrap: true,
                multiple: true,
                locals: _params,
                templateUrl: _templateUrl,
                controller: _controller,
                onShowing: function (scope, element) {
                    // กำหนดตำแหน่งการแสดงผล ให้เท่ากับ scroll ปัจจุบันของ browser
                    setTimeout(function () {
                        var positionTop = $(window).scrollTop();
                        $(element).animate({
                            top: positionTop
                        }, 200);
                    }, 200);
                }
            });
        };
    }).service('$fwDialogService', function ($mdDialog) {
        this.dangerDialog = function (event, bodyText, titleText) {
            return $mdDialog.show({
                parent: angular.element(document.body),
                clickOutsideToClose: false,
                targetEvent: event,
                openForm: (event ? event.target : null),
                skipHide: true,
                autoWrap: true,
                multiple: true,
                onShowing: function (scope, element) {
                    // กำหนดตำแหน่งการแสดงผล ให้เท่ากับ scroll ปัจจุบันของ browser
                    setTimeout(function () {
                        var positionTop = $(window).scrollTop();
                        $(element).animate({
                            top: positionTop
                        }, 0);
                    }, 200);
                },
                template:
                    '<md-dialog flex="80" flex-gt-sm="35">' +
                    '<md-toolbar>' +
                    '  <div class="md-toolbar-tools">' +
                    '    <h2>' + (titleText || 'แจ้งเตือน') + '</h2>' +
                    '  </div>' +
                    '</md-toolbar>' +
                    '	<md-dialog-content>' +
                    '		<div class="md-dialog-content">' +
                    '			<span class="text-danger f-w-900 f-16">' + bodyText + '</span>' +
                    '		</div>' +
                    '	</md-dialog-content>' +
                    '	<md-dialog-actions layout="row">' +
                    '		<span flex></span>' +
                    '		<fw-execute-button text="ปิด" css-class="btn btn-primary btn-sm m-r-5" css-icon-class="ti-close" ng-click="close()"></fw-execute-button>' +
                    '	</md-dialog-actions>' +
                    '</md-dialog>',
                controller: function ($scope, $mdDialog) {
                    $scope.close = function () {
                        $mdDialog.hide({ result: 'closed' });
                    };
                }
            });
        };

        this.alertDialog = function (event, bodyText, titleText) {
            return $mdDialog.show({
                parent: angular.element(document.body),
                clickOutsideToClose: false,
                targetEvent: event,
                openForm: (event ? event.target : null),
                skipHide: true,
                autoWrap: true,
                multiple: true,
                onShowing: function (scope, element) {
                    // กำหนดตำแหน่งการแสดงผล ให้เท่ากับ scroll ปัจจุบันของ browser
                    setTimeout(function () {
                        var positionTop = $(window).scrollTop();
                        $(element).animate({
                            top: positionTop
                        }, 0);
                    }, 200);
                },
                template:
                    '<md-dialog flex="80" flex-gt-sm="30">' +
                    '<md-toolbar>' +
                    '  <div class="md-toolbar-tools">' +
                    '    <h2>' + (titleText || 'แจ้งเตือน!!') + '</h2>' +
                    '  </div>' +
                    '</md-toolbar>' +
                    '	<md-dialog-content>' +
                    '		<div class="md-dialog-content">' +
                    '			<span class="text-info f-w-900 f-16">' + bodyText + '</span>' +
                    '		</div>' +
                    '	</md-dialog-content>' +
                    '	<md-dialog-actions layout="row">' +
                    '		<span flex></span>' +
                    '		<fw-execute-button text="ปิดหน้าต่าง" css-class="btn btn-danger btn-sm m-r-5" css-icon-class="ti-close" ng-click="close()"></fw-execute-button>' +
                    '	</md-dialog-actions>' +
                    '</md-dialog>',
                controller: function ($scope, $mdDialog) {
                    $scope.close = function () {
                        $mdDialog.hide({ result: 'closed' });
                    };
                }
            });
        }

        this.confirmDialog = function (event, bodyText, titleText) {
            return $mdDialog.show({
                parent: angular.element(document.body),
                clickOutsideToClose: false,
                targetEvent: event,
                openForm: (event ? event.target : null),
                skipHide: true,
                autoWrap: true,
                multiple: true,
                onShowing: function (scope, element) {
                    // กำหนดตำแหน่งการแสดงผล ให้เท่ากับ scroll ปัจจุบันของ browser
                    setTimeout(function () {
                        var positionTop = $(window).scrollTop();
                        $(element).animate({
                            top: positionTop
                        }, 0);
                    }, 200);
                },
                template:
                    '<md-dialog flex="80" flex-gt-sm="30">' +
                    '<md-toolbar>' +
                    '  <div class="md-toolbar-tools">' +
                    '    <h2>' + (titleText || 'ยืนยัน!!') + '</h2>' +
                    '  </div>' +
                    '</md-toolbar>' +
                    '	<md-dialog-content>' +
                    '		<div class="md-dialog-content">' +
                    '			<span class="text-primary f-w-900 f-16">' + bodyText + '</span>' +
                    '		</div>' +
                    '	</md-dialog-content>' +
                    '	<md-dialog-actions layout="row">' +
                    '		<span flex></span>' +
                    '		<fw-execute-button text="ยืนยัน" css-class="btn btn-primary btn-sm m-r-5" css-icon-class="ti-check" ng-click="ok()"></fw-execute-button>' +
                    '		<fw-execute-button text="ยกเลิก" css-class="btn btn-danger btn-sm m-r-5" css-icon-class="ti-close" ng-click="close()"></fw-execute-button>' +
                    '	</md-dialog-actions>' +
                    '</md-dialog>',
                controller: function ($scope, $mdDialog) {
                    $scope.ok = function () {
                        $mdDialog.hide({ result: 'OK' });
                    };
                    $scope.close = function () {
                        $mdDialog.cancel({ result: 'closed' });
                    };
                }
            });
        };
    }).service('$fwFormInputService', function ($mdDialog) {
        var self = this;
        // จำกัดขนาดความยาวของตัวอักษรที่สามารถระบุได้
        this.maxlength = null;
        this.openCommentForm = function (event, readonly, defaultText) {
            readonly = (undefined == readonly ? false : readonly);
            defaultText = (defaultText || '');
            return $mdDialog.show({
                parent: angular.element(document.body),
                clickOutsideToClose: false,
                targetEvent: event,
                openForm: (null != event && undefined != event ? event.target : undefined),
                skipHide: true,
                autoWrap: true,
                template:
                    '<md-dialog flex="80">' +
                    '	<md-dialog-content>' +
                    '		<div class="md-dialog-content">' +
                    '			<md-input-container class="md-block">' +
                    '				<textarea rows="6" ng-model="$settings.formData.message" placeholder="{{$settings.isReadonly?\'\':\'ระบุข้อความที่นี่ ...\'}}" ng-readonly="$settings.isReadonly"></textarea>' +
                    '				<fw-validate-error-output error-messages="$settings.validateErrors"></fw-validate-error-output>' +
                    '			</md-input-container>' +
                    '		</div>' +
                    '	</md-dialog-content>' +
                    '	<md-dialog-actions layout="row">' +
                    '		<span flex></span>' +
                    '		<fw-execute-button text="ตกลง" css-class="btn btn-primary m-r-5" css-icon-class="ti-save" ng-click="ok()" ng-if="!$settings.isReadonly"></fw-execute-button>' +
                    '		<fw-execute-button text="ยกเลิก/ปิด" css-class="btn btn-danger" css-icon-class="ion-close" ng-click="cancel()"></fw-execute-button>' +
                    '	</md-dialog-actions>' +
                    '</md-dialog>',
                controller: function ($scope, $mdDialog, $filter) {
                    $scope.$settings = {
                        isReadonly: readonly,
                        formData: { message: defaultText },
                        validateErrors: []
                    };

                    $scope.ok = function () {
                        $scope.$settings.validateErrors = [];
                        var valueText = $.trim($scope.$settings.formData.message);
                        if ('' == valueText) {
                            $scope.$settings.validateErrors.push('โปรดระบุข้อความลงในกล่องด้านบนนี้ก่อน ที่จะกดปุ่ม "ตกลง"');
                            return;
                        }
                        if (valueText.length > self.maxlength) {
                            $scope.$settings.validateErrors.push($filter('textFormat')('ความยาวตัวอักษรต้องไม่เกิน {0}', self.maxlength));
                            return;
                        }


                        $mdDialog.hide($scope.$settings.formData.message);
                    };
                    $scope.cancel = function () {
                        $mdDialog.cancel();
                    };
                }
            });
        };
    })
    .directive('fwFileUpload', function ($timeout, $http, $customHttp, $compile, $fwDialogService) {
        return {
            restrict: 'E',
            scope: {
                callback: '&?', // Pass function
                uploadUrl: '@',
                // -- Default '(pdf,xls,xlsx,doc,docx,png,jpeg,jpg)'
                acceptFiletypes: '@',
                maxFileSizeMB: '@', // Default 2M
                ngModel: '=', // Module binding for add uploaded file
                accessToken: '@',
                params: '=?',
                ngDisabled: '=?',
                multiple: '@',
                style: '@',
                oldFilename: '=?',
                icon: '@'
            },
            link: function (scope, element, attrs) {

                scope.maxFileSizeMB = (parseInt(scope.maxFileSizeMB) || 2);
                // -- Convert to byte
                scope.maxFileSizeBytes = scope.maxFileSizeMB * 1024 * 1024;
                scope.acceptFiletypes = (scope.acceptFiletypes || 'pdf,xls,xlsx,doc,docx,png,jpeg,jpg');
                scope.ignorPatternRegex = eval('/\.(' + scope.acceptFiletypes.replace(/\,/g, '|') + ')$/i');
                scope.multiple = (undefined == scope.multiple || 'false' == scope.multiple ? '' : 'multiple');
                scope.style = (undefined == scope.style ? '' : scope.style);

                // Upload component
                var inputFile = $('<input type="file" ' + scope.multiple + ' />').change(function (e) {
                    if (scope.ngDisabled) return;

                    var files = [];
                    $.each(e.target.files, function (index, file) {
                        if (file.size <= scope.maxFileSizeBytes && scope.ignorPatternRegex.test(file.name)) files.push(file);
                    });
                    var count = files.length;
                    if (count == 0) {
                        if ('' == scope.multiple)
                            $fwDialogService.dangerDialog(null, 'รูปแบบไฟล์ หรือ ขนาดไฟล์ ไม่ถูกต้อง');
                        inputFile.val('');
                        return;
                    }

                    var uploadCount = 0;
                    updateText(uploadCount, count);
                    $.each(files, function (index, file) {
                        var fread = new FileReader();
                        fread.onload = function (e) {
                            // $http.post(scope.uploadUrl, $.param({
                            // file: e.target.result,
                            // fileType: file.type,
                            // fileName: file.name
                            // }), { headers:{ "Content-Type": "application/x-www-form-urlencoded",
                            // "Authorization": 'Basic ' + scope.accessToken }})
                            $customHttp.formPost(scope.uploadUrl, {
                                fileBase64Data: e.target.result,
                                fileMimeType: file.type,
                                filename: file.name,
                                fileBytes: file.size, // byte
                                oldFilename: scope.oldFilename
                            }, scope.accessToken).then(function (res) {
                                updateText(uploadCount++, count);

                                // แสดงข้อผิดพลาด
                                if (null != res.data.errorText)
                                    $fwDialogService.dangerDialog(null, res.data.errorText);

                                // callback
                                if (angular.isFunction(scope.callback))
                                    scope.callback({ $res: res, $params: scope.params });
                                else if (undefined != scope.callback)
                                    scope.callback();

                                if (undefined != scope.ngModel && undefined != res.data.filename && $.trim(res.data.filename) != '')
                                    scope.ngModel.unshift(res.data.filename);


                                if (uploadCount >= count) defaultText();
                            }, function (res) {
                                updateText(uploadCount++, count);
                                if (uploadCount >= count) defaultText()
                                console.log('Upload error');
                            });
                        };
                        fread.readAsDataURL(file);
                    });
                });


                function updateText(current, total) {
                    if (scope.icon != 'true') {
                        template.text('Uploading ' + current + '/' + total + '(' + (current / total * 100.00).toFixed(2) + '%)');
                    }
                }
                function defaultText() {
                    if (scope.icon != 'true') {
                        // template.html('Click here to upload file
                        // ['+(scope.multiple==''?'Single File':'Multiple Files')+']
                        // - <span class="m-l-5 text-danger">' + scope.maxFileSizeMB
                        // + 'M</span><span class="m-l-10 text-warning">(Type: ' +
                        // scope.acceptFiletypes + ')</span>');
                        template.html('Click here to upload file - <span class="m-l-5 text-danger">' + scope.maxFileSizeMB + 'M</span><span class="m-l-10 text-warning">(Type: ' + scope.acceptFiletypes + ')</span>');
                        inputFile.val('');
                    }
                }

                // UI component
                var timeoutPromise = null;
                var template = $compile('<div class="d-block p-10 text-center f-w-900 align-middle" style="border:dashed 2px #ccc;cursor:pointer;{{style}}"></div>')(scope);
                if (scope.icon == 'true') {
                    template = $compile('<div class="file-upload ti-upload f-w-900 f-16 text-primary" style="cursor:pointer;"></div>')(scope);
                }

                template.click(function () {

                    if (scope.icon != 'true') {
                        if (scope.ngDisabled == true) return;
                        var self = $(this);
                        self.clearQueue().addClass('shake animated');

                        $timeout.cancel(timeoutPromise);
                        timeoutPromise = $timeout(function () {
                            self.removeClass('shake animated');
                        }, 1500);

                        inputFile.trigger('click');
                    } else {
                        if (scope.ngDisabled == true) return;
                        var self = $(this);

                        inputFile.trigger('click');
                    }
                });

                scope.$watch('ngDisabled', function (newVal, oldVal) {
                    if (newVal == true) {
                        template.addClass('text-secondary');
                        template.css({ 'Opacity': '0.4' });
                    }
                    else {
                        template.removeClass('text-secondary');
                        template.css({ 'Opacity': '1' });
                    }
                });


                defaultText();
                element.replaceWith(template);
            }
        };
    }).directive('fwValidateErrorOutput', function () {
        return {
            restrict: 'E',
            scope: {
                prefix: '@',
                errorMessages: '=',
                cssClass: '@'
            },
            link: function (scope, element, attrs) {
                scope.$watch('errorMessages', function (newVal, oldVal) {
                    var errorOutputHtml = [];
                    var errorMessages = (newVal || []);
                    $.each(errorMessages, function (index, errText) {
                        errorOutputHtml.push('<small class="text-danger fadeIn animated ' + scope.cssClass + '">' + errText + '</small>');
                    });

                    element.html('');
                    if (errorOutputHtml.length > 0) {
                        if (scope.prefix)
                            element.html('<strong class="m-r-5 text-danger f-w-600 d-inline-block">' + scope.prefix + '</strong>' + errorOutputHtml.join(""));
                        else
                            element.html(errorOutputHtml.join('<span class="m-l-5 m-r-5 f-w-900 text-danger">, </span>'));
                    }
                });
            }
        };
    }).directive('fwDateRange', function () {
        // required: daterangepicker.css, dateragepicker.js, moment.min.js
        // https://www.daterangepicker.com/
        return {
            restrict: 'E',
            replace: true,
            template: '<input type="text" class="form-control {{cssClass}}" ng-value="model" ng-disabled="disabled" placeholder="{{placeholder}}" />',
            scope: {
                model: '=',
                disabled: '=',
                showTimepicker: '@', // true, false
                singleDatePicker: '=?', // true, false แสดงปฎิทินเดียวหรือไม่ 
                change: '&?',
                placeholder: '@?',
                cssClass: '@',//,
                //format: '@' // Default DD/MM/YYYY (Uppercase Only)
            },
            controller: function ($scope, $element, $filter, $timeout) {
                var showTimePicker = $scope.showTimepicker === 'true';
                $scope.format = showTimePicker ? 'DD/MM/YYYY HH:MM' : 'DD/MM/YYYY';
                $scope.locale = $scope.$root.defaultLocale;//'th-TH'; // รูปแบบเวลา ที่ต้องการแสดงผล
                $scope.localeDateFormat = { year: 'numeric', month: '2-digit', day: '2-digit' };
                if (showTimePicker)
                    $scope.localeDateFormat = { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' };

                $scope.toLocaleDateString = function (momentDate) {
                    var date = new Date(momentDate.toLocaleString());
                    return date.toLocaleDateString($scope.locale, $scope.localeDateFormat);
                }

                $element.daterangepicker({
                    timePicker: showTimePicker,
                    timePicker24Hour: showTimePicker,
                    singleDatePicker: $scope.singleDatePicker == undefined ? false : $scope.singleDatePicker,
                    "locale": {
                        "format": "DD/MM/YYYY",
                        "separator": " - ",
                        "applyLabel": "ตกลง",
                        "cancelLabel": "ยกเลิก",
                        "fromLabel": "จากวันที่",
                        "toLabel": "ถึงวันที่",
                        "customRangeLabel": "Custom",
                        "weekLabel": "W",
                        "daysOfWeek": [
                            "อา.",
                            "จ.",
                            "อ.",
                            "พ.",
                            "พฤ.",
                            "ศ.",
                            "ส."
                        ],
                        "monthNames": [
                            "ม.ค.",
                            "ก.พ.",
                            "มี.ค",
                            "เม.ย.",
                            "พ.ค.",
                            "มิ.ย.",
                            "ก.ค.",
                            "ส.ค.",
                            "ก.ย.",
                            "ต.ค.",
                            "พ.ย.",
                            "ธ.ค."
                        ],
                        "firstDay": 1 // กำหนดให้ เริ่ม จ - อา.
                    }
                });
                // ตอนโหลด date picker มาจะแสดงผลวันที่เลยทั้งที่ยังไม่ได้ เลยต้อง Fixed แบบนี้ไว้
                $timeout(function () {
                    $scope.model = $scope.model;
                });

                // เรียก event change และให้หน่วงเวลาไว้ รอให้ model เปลี่ยนแปลงค่าก่อนค่อยเรียก
                $scope.fireChangeEvent = function () {
                    $timeout(function () {
                        if (undefined != $scope.change)
                            $scope.change();
                    }, 200);
                };

                $element.on('apply.daterangepicker', function (event, picker) {
                    $scope.$apply(function () {
                        if ($scope.singleDatePicker == true)
                            $scope.model = $scope.toLocaleDateString(picker.startDate);
                        else
                            $scope.model = $filter('textFormat')('{0} - {1}'
                                , $scope.toLocaleDateString(picker.startDate)
                                , $scope.toLocaleDateString(picker.endDate));
                        $scope.fireChangeEvent();
                    });
                });
                $element.on('hide.daterangepicker', function (event, picker) {
                    $element.val($.trim($scope.model));
                });
                $element.on('cancel.daterangepicker', function () {
                    $scope.$apply(function () {
                        $scope.model = null;
                        $scope.fireChangeEvent();
                    });
                });
                $element.on('keydown', function (event) {
                    // 8: Backspace, 46: Delete
                    if (event.keyCode === 8 || event.keyCode === 46) {
                        $scope.$apply(function () {
                            $scope.model = '';
                            $scope.fireChangeEvent();
                        });
                        return;
                    }
                    event.preventDefault();
                });
            }
        };
    }).directive('fwDateDropper', function () {
        return {
            restrict: 'EA',
            scope: {
                ngModel: '=',
                ngDisabled: '=?',
                ngReadonly: '=?',
                maskPattern: '@', // -- 9 to do (Example: 99/99/9999)
                appendClass: '@',
                dateFormat: '@', // -- refer: https://api.jqueryui.com/datepicker
                hideDay: '@', // -- true, false
                hideMonth: '@', // -- true/false
                yearRange: '@' // -- Default: 2018:c+2 (c = current year, +2 =
                // Current year +2 Years)
            }, // -- Force the directive create new scope and child scope of parent
            controller: function ($scope, $element, $attrs, $compile, $timeout) {

                $scope.maskPattern = ($scope.maskPattern || '99/99/9999');
                $scope.dateFormat = ($scope.dateFormat || 'dd/mm/yy');
                $scope.hideDay = (undefined == $scope.hideDay || '' == $scope.hideDay ? 'false' : $scope.hideDay);
                $scope.yearRange = (undefined == $scope.yearRange || '' == $scope.yearRange ? '2018:c+2' : $scope.yearRange);

                var parent = $element.parent();
                var inputGroupTemplate = $('<div class="input-group"></div>');
                if (parent.hasClass('input-group')) {
                    inputGroupTemplate = parent;
                } else {
                    inputGroupTemplate.appendTo(parent);
                }

                var inputGroupAppendTemplate = parent.find('.input-group-append');
                if (inputGroupAppendTemplate.length == 0) {
                    inputGroupAppendTemplate = $('<div class="input-group-append"></div>');
                    inputGroupAppendTemplate.appendTo(inputGroupTemplate);
                }

                var inputTemplate = $compile('<input class="form-control {{appendClass}}" ng-model="ngModel" ng-disabled="ngDisabled" ng-readonly="ngReadonly" />')($scope);
                inputTemplate.inputmask({
                    mask: $scope.maskPattern
                }).on('blur', function () {
                    if ((/(\_)/ig).test(this.value)) {
                        this.value = '';
                        $scope.$apply(function () {
                            $scope.ngModel = '';
                        });
                    }
                });
                inputTemplate.insertBefore(inputGroupAppendTemplate);

                var self = this;
                self.hideDayIfRequired = function () {
                    var uiDatePicker = $('body').find('.ui-datepicker');
                    if ('true' == $scope.hideDay)
                        uiDatePicker.addClass('ui-datepicker-hide-day');
                    else
                        uiDatePicker.removeClass('ui-datepicker-hide-day');
                };
                self.hideMonthIfRequired = function () {
                    var uiDatePicker = $('body').find('.ui-datepicker');
                    if ('true' == $scope.hideMonth)
                        uiDatePicker.addClass('ui-datepicker-hide-month');
                    else
                        uiDatePicker.removeClass('ui-datepicker-hide-month');

                };

                var btnTemplate = $compile('<button class="btn" ng-click="openCalendar()""><i class="ti-calendar"></i></button>')($scope);
                var inputCalendarTemplate = $('<input type="text" class="form-control d-none" />');
                var datePickerOptions = {
                    dateFormat: 'dd/mm/yy',
                    showButtonPanel: true,
                    changeMonth: true,
                    changeYear: true,
                    yearRange: $scope.yearRange, // '2018:c+2',
                    beforeShow: function () {
                        self.hideDayIfRequired();
                        self.hideMonthIfRequired();
                    },
                    onClose: function (dateTxt, obj) {
                        var selectedDate = new Date(obj.selectedYear, obj.selectedMonth, obj.selectedDay);
                        inputCalendarTemplate.datepicker("setDate", selectedDate);
                        $scope.$apply(function () {
                            $scope.ngModel = $.datepicker.formatDate($scope.dateFormat, selectedDate);
                        });
                    }
                };
                inputCalendarTemplate.datepicker(datePickerOptions);
                // $scope.$watch('ngModel', function(newVal){
                // inputCalendarTemplate.datepicker("setDate", newVal );
                // });
                inputCalendarTemplate.prependTo(inputGroupTemplate);
                btnTemplate.prependTo(inputGroupAppendTemplate);


                $element.remove();


                $scope.openCalendar = function () {
                    if ($scope.ngDisabled) return;
                    inputCalendarTemplate.datepicker("show");
                };
            }
            // link: function(scope, element, attrs){
            //			
            // $timeout(function(){
            // $(element).dateDropper( {
            // dropWidth: 200,
            // format: (attrs.fwDateDropper || 'd/m/Y'),
            // hideDay: (angular.isDefined(attrs.hideDay) && 'true' == attrs.hideDay),
            // hideMonth: (angular.isDefined(attrs.hideMonth) && 'true' == attrs.hideMonth),
            // hideYear: (angular.isDefined(attrs.hideYear) && 'true' == attrs.hideYear),
            // dropPrimaryColor: "#e74c3c",
            // dropBorder: "1px solid #e74c3c",
            // minYear: 2017
            // });
            // });
            // }
        };
    }).directive('fwInputMask', function () {
        // required: [inputmask.js, jquery.inputmask.js]
        return {
            restrict: 'E',
            replace: true,
            template: '<input type="text" class="form-control {{class}}" style="{{style}}" ng-model="model" ng-change="change()" placeholder="{{placeholder}}" ng-disabled="disabled" />',
            scope: {
                class: '@?',
                disabled: '=?',
                model: '=',
                mask: '@?', // Default: 99/99/9999
                placeholder: '@?',
                change: '&?',
                style: '@?' // css style
            },
            link: function (scope, element) {
                scope.mask = scope.mask || '99/99/9999';
                element.inputmask({
                    mask: scope.mask
                })
            }
        }
    }).directive('fwTimepicker', function () {
        // required: Timepicki
        // http://senthilraj.github.io/TimePicki/options.html
        return {
            restrict: 'E',
            replace: true,
            template: '<input type="text" class="form-control" ng-model="model" ng-disabled="disable" />',
            scope: {
                model: '=',
                disable: '=',
                placeholder: '@'
            },
            link: function (scope, element) {
                element.timepicki({
                    disable_keyboard_mobile: false,
                    show_meridian: false
                });
            }
        };
    }).directive('fwSelect2', function ($timeout) {
        return {
            restrict: 'EA',
            scope: {
                maxSelection: '@?',
                placeholder: '@?',
                ngModel:'=?'
            },
            link: function (scope, element, attrs) {
                scope.maxSelection = (parseInt(scope.maxSelection) || undefined);
                scope.placeholder = (scope.placeholder || '');
                scope.bind = function () {
                    $timeout(function () {
                        $(element).select2({
                            maximumSelectionLength: scope.maxSelection,
                            placeholder: scope.placeholder
                        });
                    });
                };

                if (scope.ngModel != null)
                    scope.$watch('ngModel', function () {
                        scope.bind();
                    });

                scope.bind();
            }
        };
    }).directive('fwButtonSearch', function ($compile, $timeout) {
        return {
            restrict: 'E',
            scope: {
                text: '@?',
                onLoading: '=',
                ngClick: '&?',
                cssClass: '@?'
            },
            link: function (scope, element, attrs) {
                scope.text = (scope.text || 'Search');
                scope.cssClass = (scope.cssClass || 'btn btn-primary');

                var timeoutPromise = null;
                scope.fireClick = function ($event) {
                    template.addClass('animated bounceIn');
                    $timeout.cancel(timeoutPromise);
                    timeoutPromise = $timeout(function () {
                        template.removeClass('animated bounceIn');
                    }, 1500);
                    if (angular.isFunction(scope.ngClick)) scope.ngClick({ $event: $event });
                };
                var template = $compile('<button type="button" class="' + scope.cssClass + '" ng-click="fireClick($event)" ng-disabled="onLoading"><i ng-class="{\'ti-reload rotate-refresh\': onLoading, \'ti-search\': !onLoading}"></i>&nbsp;{{text}}</button>')(scope);
                element.replaceWith(template);
            }
        };
    }).directive('fwExecuteButton', function ($compile, $timeout, $window) {
        return {
            restrict: 'E',
            scope: {
                text: '@?',
                onLoading: '=',
                ngDisabled: '=?',
                ngClick: '&?',
                cssClass: '@?',
                cssIconClass: '@?',
                cssAnimate: '@?',
                linkTo: '@?',
                type: '@?',
            },
            link: function (scope, element, attrs) {
                scope.text = (scope.text || 'Execute');
                scope.cssClass = (scope.cssClass || 'btn btn-primary btn-sm');
                scope.cssIconClass = (scope.cssIconClass || 'ion-flash');
                scope.cssAnimate = (scope.cssAnimate || 'bounceIn');
                scope.type = (scope.type || 'button');

                var timeoutPromise = null;
                scope.fireClick = function ($event) {
                    template.addClass('animated ' + scope.cssAnimate);
                    $timeout.cancel(timeoutPromise);
                    timeoutPromise = $timeout(function () {
                        template.removeClass('animated ' + scope.cssAnimate);
                        if (angular.isDefined(scope.linkTo))
                            $window.location.href = scope.linkTo;
                    }, 500);

                    if (angular.isFunction(scope.ngClick))
                        scope.ngClick({ $event: $event });
                };

                var template = $compile('<button type="{{type}}" class="{{cssClass}}" ng-click="fireClick($event)" ng-disabled="onLoading||ngDisabled"><i ng-class="{\'ti-reload rotate-refresh\': onLoading, \'' + scope.cssIconClass + '\': !onLoading}"></i><span ng-if="text!=\'\'&&null!=text">&nbsp;{{text}}</span></button>')(scope);
                element.replaceWith(template);
            }
        };
    }).directive('fwInputNumberMask', function ($compile, $timeout) { // -- Required
        // autoNumeric.js
        return {
            restrict: 'E',
            replace: false,
            scope: {
                ngModel: '=',
                ngDisabled: '=?',
                ngChange: '&?',
                ngClass: '@',
                placeholder: '@?',
                symbolPosition: '@?', // -- p = prefix, s = suffix (Default with
                // p)
                symbol: '@?',
                minValue: '@?',
                maxValue: '@?',
                cssClass: '@?',
                thousandDelimiter: '@',
                style: '@',
            },
            link: function (scope, element, attrs) {
                scope.ngDisabled = (angular.isDefined(scope.ngDisabled) ? scope.ngDisabled : false);
                scope.symbol = (angular.isDefined(scope.symbol) ? scope.symbol : '');
                scope.symbolPosition = (angular.isDefined(scope.symbolPosition) ? scope.symbolPosition : 'p');
                scope.minValue = (angular.isDefined(scope.minValue) ? scope.minValue : '0.00');
                scope.maxValue = (angular.isDefined(scope.maxValue) ? scope.maxValue : '99999999999999999.99'); // --
                // 17
                // digits
                scope.thousandDelimiter = (angular.isDefined(scope.thousandDelimiter) ? scope.thousandDelimiter : ',');
                scope.style = (scope.style || '');

                var fireUpdateTimeoutId = null;
                scope.fireUpdate = function (callback) {
                    $timeout.cancel(fireUpdateTimeoutId);
                    fireUpdateTimeoutId = $timeout(function () {
                        // var value = (parseFloat(template.autoNumeric('get')) || 0);
                        // if(value != scope.ngModel)
                        // scope.ngModel = value;
                        // if(angular.isFunction(callback))
                        // callback.call();
                        var value = template.autoNumeric('get');
                        // if (value != scope.ngModel)
                        scope.ngModel = value;
                        // console.log(value);
                        if (angular.isFunction(callback))
                            callback.call();
                    }, 10);
                };


                var template = $compile('<input type="text" class="form-control {{cssClass}}" ng-disabled="ngDisabled" style="{{style}}" placeholder="{{placeholder}}" />')(scope);
                template.autoNumeric('init', {
                    aSep: scope.thousandDelimiter,
                    aSign: scope.symbol,
                    pSign: scope.symbolPosition,
                    vMin: scope.minValue,
                    vMax: scope.maxValue
                }).on('keypress', function () {
                    scope.fireUpdate(function () {
                        if (angular.isFunction(scope.ngChange))
                            scope.ngChange.call();//(scope.$root.$$childHead);
                    });
                }).on('change', function () {
                    scope.fireUpdate(function () {
                        if (angular.isFunction(scope.ngChange))
                            scope.ngChange.call();//(scope.$root.$$childHead);
                    });
                }).on('blur', function () {
                    scope.fireUpdate(function () {
                        if (angular.isFunction(scope.ngChange))
                            scope.ngChange.call();//(scope.$root.$$childHead);
                    });
                }).on('focusin', function () {
                    $(this).select();
                });

                var timeoutId = null;
                scope.$watch('ngModel', function (newVal, oldVal) {
                    // var value = (parseFloat(template.autoNumeric('get')) || 0);
                    // if(value != newVal)
                    // template.autoNumeric('set', (null == newVal || undefined == newVal ? '' :
                    // newVal));
                    var value = template.autoNumeric('get');// (parseFloat(template.autoNumeric('get'))
                    // || 0);
                    if (value != newVal)
                        template.autoNumeric('set', (null == newVal || undefined == newVal ? '' : newVal));
                });

                if (null != scope.ngModel && undefined != scope.ngModel)
                    template.autoNumeric('set', scope.ngModel);
                element.replaceWith(template);
            }
        };
    }).directive('fwFormWizard', function () {
        var formWizardId = new Date().getTime() + Math.floor(Math.random());
        return {
            restrict: 'E',
            transclude: true,
            replace: true,
            template:
                '<div class="wizard clearfix" id="' + formWizardId + '">' +
                '	<div class="steps clearfix">' +
                '		<ul role="tablist" class="d-flex o-auto">' +
                '			<li role="tab" class="w-auto text-center" ng-repeat="wizard in $settings.wizards" ng-class="{\'d-none\': !wizard.visible, \'d-inline-block\': wizard.visible, \'x-disabled\': wizard.disabled, \'current\': $index===$settings.wizardIndex, \'done\': $index <= stepIndex && $index!=$settings.wizardIndex && !wizard.disabled, \'disabled\': $index > stepIndex || wizard.disabled}">' +
                '				<a href="javascript:void(0)" ng-click="getContent($index, \'SHEET\')">{{wizard.title}}</a>' +
                '			</li>' +
                '		</ul>' +
                '	</div>' +
                '	<div class="actions clearfix" style="border-top:11px solid #ccc;">' +
                '		<fw-execute-button text="" ng-click="prev()" css-class="btn btn-info" ng-disabled="onLoading" css-icon-class="icon-arrow-left" ng-disabled="$settings.wizardIndex==0"></fw-execute-button>' +
                '		<fw-execute-button text="" ng-click="next()" css-class="btn btn-info" on-loading="onLoading" css-icon-class="icon-arrow-right" ng-disabled="$settings.wizardIndex==($settings.wizards.length-1)"></fw-execute-button>' +
                '	</div>' +
                '	<div class="content clearfix" ng-transclude style="border-top:none;"></div>' +
                '</div>',
            scope: {
                startIndex: '@?',
                validateFunc: '&?',
                onLoading: '=?',
            },
            controller: ['$scope', '$timeout', function fwFormWizardController(scope, $timeout) {
                var self = this;
                self.startStepIndex = (parseInt(scope.startIndex) || 0);
                self.countWizard = 0;
                self.put = function (titleText, disabled, isVisible) {
                    scope.$settings.wizards.push({
                        title: titleText,
                        disabled: disabled,
                        visible: isVisible
                    });
                    self.countWizard = scope.$settings.wizards.length;
                };

                scope.stepIndex = self.startStepIndex;
                scope.$settings = {
                    wizards: [],
                    wizardIndex: self.startStepIndex
                };


                var contentContainer = angular.element('div#' + formWizardId).children('div.content');
                var sheetContainer = angular.element('div#' + formWizardId).children('div.steps');
                scope.getContent = function (index, type) {
                    var sheetEl = sheetContainer.find('ul > li:eq(' + index + ')');
                    if (sheetEl.length == 0 || ('SHEET' == type && sheetEl.hasClass('disabled')) || sheetEl.hasClass('x-disabled'))
                        return false;

                    scope.stepIndex = (scope.stepIndex < index ? index : scope.stepIndex);
                    scope.$settings.wizardIndex = index;
                    contentContainer.children('div#wizard-item').each(function (elementIndex, element) {
                        var self = $(element);
                        if (elementIndex === index) {
                            self.removeClass('d-none');
                            self.addClass('animated fadeIn')
                        } else {
                            self.addClass('d-none');
                            self.removeClass('animated fadeIn');
                        }
                    });
                };


                scope.next = function () {
                    if (angular.isFunction(scope.validateFunc)) {
                        scope.validateFunc({ stepIndex: scope.$settings.wizardIndex }).then(function (res) {
                            res.allowNext = (angular.isDefined(res.allowNext) ? res.allowNext : true);
                            if (res.allowNext) self.doAction('NEXT');
                        }, function () { });
                    } else {
                        self.doAction('NEXT');
                    }
                };
                scope.prev = function () {
                    self.doAction('PREV');
                };
                self.doAction = function (type) {
                    var result = false;
                    do {
                        if ('NEXT' == type) {
                            scope.$settings.wizardIndex++;
                            if (scope.$settings.wizardIndex > self.countWizard) {
                                scope.$settings.wizardIndex = -1;
                                scope.getContent(self.startStepIndex);
                                break;
                            }
                        } else {
                            scope.$settings.wizardIndex--;
                            if (scope.$settings.wizardIndex < 0) {
                                scope.$settings.wizardIndex = -1;
                                scope.getContent(self.startStepIndex);
                                break;
                            }
                        }

                        result = scope.getContent(scope.$settings.wizardIndex);
                    } while (false == result);
                };


                $timeout(function () {
                    scope.getContent(scope.stepIndex);
                }, 100);
            }]
        };
    }).directive('fwWizardItem', function () {
        return {
            restrict: 'E',
            require: '^^fwFormWizard',
            template: '<div ng-transclude class="d-none" id="wizard-item"></div>',
            transclude: true,
            replace: true,
            scope: {
                label: '@',
                disabled: '=?',
                visible: '=?'
            },
            link: function (scope, element, attrs, fwFormWizard) {
                scope.disabled = (angular.isDefined(scope.disabled) ? scope.disabled : false);
                scope.visible = (angular.isDefined(scope.visible) ? scope.visible : true);
                fwFormWizard.put(scope.label, scope.disabled, scope.visible);
            }
        };
    }).directive('fwEditor', function ($timeout) { // -- Required tinymce
        return {
            restrict: 'A',
            scope: {
                ngDisabled: '=?',
                ngModel: '=',
                defaultHeight: '@?'
            },
            link: function (scope, element, attrs) {
                scope.ngDisabled = (angular.isDefined(scope.ngDisabled) ? scope.ngDisabled : false);
                scope.defaultHeight = (parseInt(scope.defaultHeight) || 250);

                var editor = null;

                var t1 = null;
                scope.setDisable = function () {
                    $timeout.cancel(t1);
                    t1 = $timeout(function () {
                        if (null !== editor)
                            editor.setMode(scope.ngDisabled ? 'readonly' : 'design');
                    }, 700);
                };

                scope.$watch('ngDisabled', function (newVal, oldVal) {
                    //if(null != editor)
                    // 	editor.setMode(newVal ? 'readonly' : 'design');
                    scope.setDisable();
                });


                scope.$watch('ngModel', function (newVal, oldVal) {
                    if (null !== editor) {
                        try {
                            scope.setDisable();
                            var editVal = editor.getContent();
                            if (editVal !== newVal)
                                editor.setContent(null === newVal ? '' : newVal);
                        } catch (error) {
                            console.log(error);
                        }
                    }
                });



                $timeout(function () {
                    tinymce.init({
                        selector: attrs.fwEditor,
                        height: scope.defaultHeight,
                        theme: 'modern',
                        menubar: false,
                        resize: false,
                        plugins: [ // -- wordcount
                            'advlist autolink lists link image charmap print preview hr anchor pagebreak',
                            'searchreplace visualblocks visualchars code fullscreen',
                            'insertdatetime media nonbreaking save table contextmenu directionality',
                            'emoticons template paste textcolor colorpicker textpattern imagetools toc'
                        ],
                        toolbar1: 'undo redo | styleselect | bold italic | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | print preview | forecolor backcolor emoticons | codesample', // |
                        // link
                        // image',
                        // toolbar2: 'print preview | forecolor backcolor
                        // emoticons
                        // | codesample',
                        image_advtab: false,
                        setup: function (ed) {
                            editor = ed;
                            //editor.setMode(scope.ngDisabled ? 'readonly' : 'design');
                            ed.on('keyup change', function () {
                                $timeout(function () {
                                    scope.ngModel = ed.getContent();
                                });
                            });
                        }
                    });
                }, 100);
            }
        };
    })
    .directive('fwLink', function ($compile, $timeout, $window) {
        return {
            restrict: 'E',
            template: '<a class="{{cssClass}}" ng-click="fireClick()" style="cursor:pointer;"><i ng-if="cssIconClass" class="{{cssIconClass}} mr-2"></i>{{text}}</a>',
            scope: {
                text: '@',
                linkTo: '@',
                cssClass: '@',
                cssIconClass: '@',
                ngClick: '&?'
            },
            link: function (scope, element, attrs) {
                scope.isClick = false;
                scope.fireClick = function () {
                    if (scope.isClick) return;
                    scope.isClick = true;
                    element.addClass('animated zoomOut');

                    var isCallback = angular.isFunction(scope.ngClick);
                    if (isCallback)
                        scope.ngClick();

                    $timeout(function () {
                        element.removeClass('animated zoomOut');
                        if (!isCallback)
                            $window.location.href = scope.linkTo;
                    }, 100);
                };
            }
        };
    })
    .directive('fwTagsInput', function () {
        // Required: jquery.tagsinput-revisited.js, jquery.tagsinput-revisited.css
        // ค่าที่ได้กลับไปและส่งเข้ามา จะคั่นด้วย Comma(,)
        return {
            restrict: 'E',
            replace: true,
            template: '<input type="text" id="tagsInput" name="tagsInput" />',
            scope: {
                model: '=',
                sources: '=', // ข้อมูลที่ต้องการ กำหนดให้ระบุค่าลงไปเพื่อทำเป็น tags
                disabled: '=?',
                change: '&?', // เมื่อมีการเปลี่ยนแปลงข้อมูลใน Component
                placeholder: '@?', // ข้อความที่ต้องการแสดง ช่องที่ให้พิมพ์ค้นหา (Default: เพิ่มรายการ)
            },
            controller: function ($scope, $element) {
                $scope.selectedVals = ($scope.model || '').split(',').map(function (val) {
                    return $.trim(val);
                }).filter(function (val) { return val != ''; });

                $element.tagsInput({
                    autocomplete: {
                        source: $scope.sources || []
                    },
                    placeholder: $scope.placeholder ? $scope.placeholder : 'เพิ่มรายการ',
                    // สามารถเลือกได้เฉพาะในรายการที่กำหนดเท่านั้น
                    whitelist: $scope.sources || undefined,
                    // เมื่อมีการเพิ่ม Tag ใหม่เข้ามา
                    onAddTag: function (input, value) {
                        $scope.$apply(function () {
                            $scope.getInputTag('ADD', value);
                        });
                    },
                    // เมื่อมีการลบ Tag ออกไป
                    onRemoveTag: function (input, value) {
                        $scope.$apply(function () {
                            $scope.getInputTag('REMOVE', value);
                        });
                    }
                });
                $scope.getInputTag = function (type, value) {
                    if ($scope.disable)
                        return;

                    var foundIndex = $scope.selectedVals.indexOf(value);
                    if ('REMOVE' == type && foundIndex > -1)
                        $scope.selectedVals.splice(foundIndex, 1);
                    else if ('ADD' == type && foundIndex == -1)
                        $scope.selectedVals.push(value);

                    if ($scope.change != undefined)
                        $scope.change();

                    $scope.model = $scope.selectedVals.join(',');
                };
            }
        };
    })
    .filter('textFormat', [function () {
        return function (input) {
            if (arguments.length > 1) {
                // If we have more than one argument (insertion values have been given)
                var str = input;
                // Loop through the values we have been given to insert
                for (var i = 1; i < arguments.length; i++) {
                    // Compile a new Regular expression looking for {0}, {1} etc in the
                    // input string
                    var reg = new RegExp("\\{" + (i - 1) + "\\}");
                    // Perform the replace with the compiled RegEx and the value
                    str = str.replace(reg, arguments[i]);
                }
                return str;
            }

            return input;
        };
    }])
    .filter('toMonthLabel', function ($rootScope) {
        return function (monthNo) {
            if (null == monthNo || undefined == monthNo)
                return '-';

            var labelEn = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
            var labelTh = ['ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.', 'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.'];
            monthNo = (+monthNo);
            return $rootScope.defaultLocale == 'th-TH' ? labelTh[monthNo - 1] : labelEn[monthNo - 1];
        }
    })
    .filter('fwUrlEncoded', function () {
        return function (text) {
            text = (text || '');
            if (typeof encodeURIComponent == 'function')
                return encodeURIComponent(text);
            else
                return encodeURI(text);
        };
    })
    .filter('fwSimpleSummary', function ($filter) {
        // fields = รายชื่อฟิลด์ที่ใช้ในการรวมจำนวน
        // decimalDigits = จำนวนตำแหน่งทศนิยม
        // expr = เงื่อนไขที่ใช้ตรวจเช็ค ก่อนจะคำนวณค่า {ชื่อฟิลด์: เงื่อนไข, ...}
        // isNumberFormat = ค่าที่ Return จะให้ Format เป็นข้อความก่อนหรือไม่
        return function (items, fields, decimalDigits, expr, isNumberFormat) {
            var output = 0;
            if (undefined == items || null == items || items.length == 0 || undefined == fields || null == fields || fields.length == 0)
                return output;

            expr = JSON.parse((expr || '{}'));
            var keys = Object.keys(expr);

            angular.forEach(items, function (item) {
                var isAllow = true;
                angular.forEach(keys, function (key) {
                    if (item[key] != expr[key])
                        isAllow = false;
                });

                if (isAllow)
                    angular.forEach(fields, function (fieldName) {
                        output += (Number(item[fieldName]) || 0);
                    });
            });

            if (isNumberFormat || undefined == isNumberFormat || null == isNumberFormat)
                return (undefined == decimalDigits || null == decimalDigits ? output : $filter('number')(output, decimalDigits));

            return output;
        };
    }).directive('fwSimpleDataTable', function () {
        // Tips: 
        //      1.) if params change you can call "$scope.$broadcast('fwSimpleDataTable.paramChanged', params)"

        return {
            restrict: 'E', // Element Only
            replace: true,
            template:
                '<div class="d-block m-t-5 m-b-5 {{class}}">' +
                '    <div class="row " ng-class="{\'d-none\': visiblePageSize==false}">' +
                '        <div class="col-12">' +
                '            <div class="input-group input-group-sm mb-1">' +
                '                <div class="input-group-prepend"><span class="input-group-text">แสดง</span></div>' +
                '                <select class="form-control text-center" style="min-width:70px;width:70px;max-width:70px" ng-change="retrieve()" ng-disabled="disabled" ng-model="pageSize" ng-options="num as num for num in pagesLimit"></select>' +
                '                 <div class="input-group-append">' +
                '                    <span class="input-group-text f-w-900">รายการ จากทั้งหมด {{totalRecords}} รายการ</span>' +
                '                </div>' +
                '            </div>' +
                '        </div>' +
                '    </div>' +
                '' +
                '    <div class="table-responsive">' +
                '        <table class="table table-striped table-hover table-bordered mb-0">' +
                '            <tr ng-if="null!=title">' +
                '                <th colspan="{{columns.length}}" class="bg-light text-primary text-left">{{title}}</th>' +
                '            </tr>' +
                '            <tr>' +
                '                <th ng-repeat="column in columns" class="{{column.className}}" style="{{column.style}}" compile="column.label" ng-click="columnClick($event, $index, column)"></th>' +
                '            </tr>' +
                '			<tr ng-if="!disabled&&rows.length==0"><th colspan="{{columns.length}}" class="text-center text-danger animated fadeIn">--- ไม่พบข้อมูล ---</th></tr>' +
                '            <tr ng-repeat="(rowIndex, row) in rows">' +
                '                <td ng-repeat="column in columns" class="{{column.className}}" style="{{column.style}}" ng-click="click($event, rowIndex, row, column)" ng-class="{{column.classExpression}}" compile="(row|_privateGetValueFromField:column:rowIndex)">' +
                '				 </td>' +
                '            </tr>' +
                '        </table>' +
                '    </div>' +
                '' +
                '    <div class="row m-t-5" ng-class="{\'d-none\': visiblePaging==false}">' +
                '        <div class="col-12">' +
                '            <div class="input-group input-group-sm">' +
                '                <div class="input-group-prepend">' +
                '                    <button class="btn btn-primary" ng-disabled="disabled||currPage==1" ng-click="prev()">Prev</button>' +
                '                </div>' +
                '                <input type="text" class="form-control text-center" style="min-width:40px;width:50px;max-width:70px;" ng-model="currPage" ng-disabled="disabled" ng-change="retrieve()" maxlength="5"/>' +
                '                <div class="input-group-append f-weight-900">' +
                '                    <span class="input-group-text f-w-900">Of {{totalPages}} Pages</span>' +
                '                </div>' +
                '                <div class="input-group-append">' +
                '                    <button class="btn btn-primary" ng-disabled="disabled||totalPage==1||totalPage==0" ng-click="next()">Next</button>' +
                '                </div>' +
                '            </div>' +
                '        </div>' +
                '    </div>' +
                '</div>'
            ,
            scope: {
                // required
                columns: '=', // array [{label: 'col1', className: 'class name',
                // style: 'css style', field: 'field1' or ['', '', ...], type:
                // 'field,rowNumber,button,fields,fieldNumber,html,expression'
                //  expression exp: <span ng-if="row.xxxx">value</span>
                // , format: '{0}, {1}, {2}, ...' รูปแบบการแสดงผลข้อมูล
                // , params: ค่าต่างๆที่ต้องการผ่านเข้ามา และจะผ่านกลับไปที่ rowClickCallback(columnConfig)
                // , classExpression: ต้องการแสดง Class ด้วยเงื่อนไขต่างๆ ให้ใช้ row.[ตามด้วยชื่อ Field จาก json]
                routeUrl: '@', // Url Retrieve
                httpToken: '@',

                // Optional
                params: '=?', // object {key1: val1, key2: val2, ...}
                pagesLimit: '=?', // array default: [10,20,30,40]
                disabled: '=?',
                title: '@?',
                class: '@?',
                autoLoad: '=?', // true,false (default: true) ต้องการให้โหลดข้อมูลหลังจากวาดตารางเสร็จ?
                // ชื่อเหตุการณ์ เมื่อมีการเปลี่ยนแปลงเงื่อนไขในฟอร์มการค้นหา จะให้ Reload ข้อมูลให้ใหม่ (Default: "fwSimpleDataTable.paramsChanged")
                // เรียกโดยจาก Controller โดย $scope.$broadcast(eventName, params:Object [Ex. { key: val, ... }])
                eventName: '@?', // Default: fwSimpleDataTable.paramsChanged
                visiblePageSize: '=?', // true,false (Default: true)
                visiblePaging: '=?', // true,false (Default: true)
                // callback when row is clicked: required return promise
                // pattern: callback($event, rowIndex, row, columnConfig)
                rowClickCallback: '&?', // required return promise
                columnClickCallback: '&?', // required return promise {columnConfig: ..., }
                searchDoneCallback: '&?' // callback(res), required return promise resolve(res), reject(xx) (.then)
            },
            controller: function ($scope, $timeout, $customHttp) {
                $scope.pagesLimit = angular.isArray($scope.pagesLimit) ? $scope.pagesLimit : [5, 10, 20, 30, 40];
                $scope.columns = angular.isArray($scope.columns) ? $scope.columns : [];
                $scope.title = $scope.title || null;
                $scope.params = angular.isObject($scope.params) ? $scope.params : {};
                $scope.autoLoad = angular.isUndefined($scope.autoLoad) ? true : $scope.autoLoad;
                $scope.eventName = $scope.eventName || 'fwSimpleDataTable.paramsChanged';

                $scope.pageSize = 10;
                $scope.currPage = 1;
                $scope.totalPages = 0;
                $scope.totalRecords = 0;
                $scope.rows = [];

                // column click
                $scope.columnClick = function (event, index, columnConfig) {
                    if ($scope.columnClickCallback === undefined)
                        return;
                    if (!angular.isFunction($scope.columnClickCallback)) {
                        $scope.columnClickCallback();
                        return;
                    }

                    if ('html' === columnConfig.type)
                        $scope.columnClickCallback({ $event: event, $index: index, $columnConfig: columnConfig, $rows: $scope.rows }).then(function (res) {
                            $scope.columns[index] = res.columnConfig || $scope.columns[index];
                            $scope.rows = res.rows || $scope.rows;
                        });
                };
                // row click
                $scope.click = function (event, index, row, columnConfig) {
                    if (columnConfig.type === 'html' && $scope.rowClickCallback !== undefined)
                        $scope.rowClickCallback({ $event: event, $index: index, $row: row, $columnConfig: columnConfig }).then(function (res) {
                            $scope.rows[index] = $.extend({}, res.row);
                        });
                };

                $scope.retrieve = function () {
                    $scope.currPage = (parseInt($scope.currPage) || 0);
                    if ($scope.currPage > $scope.totalPages)
                        $scope.currPage = ($scope.totalPages > 0 ? $scope.totalPages : 1);
                    if ($scope.currPage <= 0)
                        $scope.currPage = 1;
                    $scope.disabled = true;

                    var params = $.extend(true, {
                        pageIndex: $scope.currPage,
                        pageSize: $scope.pageSize
                    }, $scope.params);
                    $customHttp.formPost($scope.routeUrl, params, $scope.httpToken).then(function (res) {
                        if (angular.isFunction($scope.searchDoneCallback))
                            $scope.searchDoneCallback({ $res: res }).then(function (res) {
                                $scope.rows = (res.data.rows || []);
                                $scope.totalPages = (res.data.totalPages || 0);
                                $scope.totalRecords = (res.data.totalRecords || 0);
                                $scope.disabled = false;
                            }, function () {
                                $scope.disabled = false;
                            });
                        else {
                            if (undefined != $scope.searchDoneCallback)
                                $scope.searchDoneCallback();

                            $scope.rows = (res.data.rows || []);
                            $scope.totalPages = (res.data.totalPages || 0);
                            $scope.totalRecords = (res.data.totalRecords || 0);
                            $scope.disabled = false;
                        }
                    }, function () {
                        $scope.disabled = false;
                    });
                };

                $scope.prev = function () {
                    $scope.currPage--;
                    $scope.retrieve();
                };
                $scope.next = function () {
                    $scope.currPage++;
                    $scope.retrieve();
                };

                // param change from parent scope
                // default: fwSimpleDataTable.paramsChanged
                $scope.$on($scope.eventName, function (event, params) {
                    $scope.params = (params || {});
                    $scope.currPage = 1;
                    $scope.retrieve();
                });


                // ต้องการให้โหลดข้อมูลให้ หลังจากวาดตารางเสร็จ?
                if ($scope.autoLoad) {
                    $scope.disabled = true;
                    $timeout(function () {
                        $scope.currPage = 1;
                        $scope.retrieve();
                    }, 100);
                }
            }
        };
    }).directive('compile', ['$compile', function ($compile) {
        return function (scope, element, attrs) {
            scope.$watch(
                function (scope) {
                    // watch the 'compile' expression for changes
                    return scope.$eval(attrs.compile);
                },
                function (value) {
                    // when the 'compile' expression changes
                    // assign it into the current DOM
                    element.html(value);

                    // compile the new DOM and link it to the current
                    // scope.
                    // NOTE: we only compile .childNodes so that
                    // we don't get into infinite loop compiling ourselves
                    $compile(element.contents())(scope);
                }
            );
        };
    }]).filter('_privateGetValueFromField', function ($filter) { // Only for "fwSimpleDataTable"
        return function (currRow, columnConfig, rowIndex) {
            var fieldType = columnConfig.type; // field,rowNumber,button,fields,fieldNumber
            var self = this;
            self.getValue = function (fieldName) {
                var fieldNames = fieldName.split('.');
                var value = "";
                var tmpRow = $.extend(true, {}, currRow);
                angular.forEach(fieldNames, function (field) {
                    value = tmpRow[field];
                    tmpRow = value;
                });
                return value;
            };

            // values is array
            self.formatValue = function (values) {
                var format = $.trim(columnConfig.format); // null, undefined equals ''
                if ('' === format)
                    return values.join('');

                var output = format;
                for (var i = 0; i <= values.length; i++) {
                    var regexp = new RegExp("\\{" + i + "\\}");
                    output = output.replace(regexp, values[i]);
                }
                return output;
            };


            if ('html' === fieldType || 'expression' === fieldType) {
                return columnConfig.field;
            } else if ('field' === fieldType)
                return self.formatValue([self.getValue(columnConfig.field)]);
            else if ('fieldNumber' === fieldType)
                return self.formatValue([$filter('number')(self.getValue(columnConfig.field), 2)]);
            else if ('rowNumber' === fieldType)
                return self.formatValue([rowIndex + 1]);
            else if ('fields' === fieldType && angular.isArray(columnConfig.field)) {
                var results = [];
                angular.forEach(columnConfig.field, function (field) {
                    if (null === field || '' === $.trim(field))
                        results.push(field);
                    else
                        results.push(self.getValue(field));
                });
                return self.formatValue(results);
            }
        };
    }).directive('fwTabs', function () {
        return {
            restrict: 'E',
            transclude: true,
            replace: true,
            scope: {
                defaultIndex: '@', // start with 1
                size: '@', // ''(empty), sm,lg
                css: '@'
            },
            template:
                '<div class="container-fluid m-0 p-0" id="{{$settings.uniqueId}}">' +
                '   <div class="btn-group btn-group-{{size}} {{css}} mb-2 border-bottom border-bottom-default d-flex d-md-block overflow-auto">' +
                '       <a class="btn bg-none pl-4 pr-4" ng-repeat="tab in $settings.tabs" ng-class="{\'text-secondary\': tab.disabled, \'text-primary f-w-900 border-bottom-primary-2 animated slideInLeft\': tab.active, \'text-mute\': !tab.active}" ng-click="activeTab($index)" style="min-width:90px;"><i ng-if="tab.cssIcon" class="{{tab.cssIcon}} mr-2"></i>{{tab.label}}</a>' +
                '   </div><div class="clearfix"></div>' +
                '   <div id="tabContentContainer" ng-transclude></div>' +
                '</div>',
            controller: function ($scope, $filter, $timeout) {
                $scope.defaultIndex = (+$scope.defaultIndex || 1) - 1;
                $scope.size = $.trim($scope.size) === '' ? '' : $scope.size;

                $scope.$settings = {
                    uniqueId: $filter('textFormat')('fwTab_{0}', new Date().getTime() + Math.floor(Math.random())),
                    tabs: [] // {cssIcon: '', active: true/false, disabled: true/false, label: '', callback: function return promise}
                };


                // Callback (if required) and then do something with tab
                $scope.activeTab = function (index) {
                    var tabContainer = $($filter('textFormat')('div#{0}', $scope.$settings.uniqueId));

                    var nextTab = $scope.$settings.tabs[index];
                    if (nextTab.disable)
                        return;


                    if (angular.isFunction(nextTab.callback)) {
                        // รูปแบบการเรียกใช้งาน Angular จะ Map ชื่อพารามิเตอร์ให้ตามที่ระบุเข้ามาจาก callback
                        // callback($index), callback($currentTab), callback($index, $currentTab)
                        nextTab.callback({ $index: index, $currentTab: nextTab }).then(function (resTab) {
                            $scope.$settings.tabs[index] = (resTab || nextTab);
                            var currTab = $scope.$settings.tabs[index];
                            if (!currTab.disabled) {
                                angular.forEach($scope.$settings.tabs, function (tab) {
                                    tab.active = false;
                                });
                                currTab.active = true;

                                // Hide all tab content
                                tabContainer.find('div#tabContentContainer > div#fwTabItem').addClass('d-none');
                                // Show current tab content
                                tabContainer.find('div#tabContentContainer > div#fwTabItem:eq(' + index + ')').removeClass('d-none');
                            }
                        });
                    } else {
                        if (null !== nextTab.callback && undefined !== nextTab.callback)
                            nextTab.callback();

                        angular.forEach($scope.$settings.tabs, function (tab) {
                            tab.active = false;
                        });
                        nextTab.active = true;

                        // Hide all tab content
                        tabContainer.find('div#tabContentContainer > div#fwTabItem').addClass('d-none');
                        // Show current tab content
                        tabContainer.find('div#tabContentContainer > div#fwTabItem:eq(' + index + ')').removeClass('d-none');
                    }
                };

                // label: text, disabled: true/false, active: true/false
                // params: null/{}/...
                // callback: function(tab object) and required to return promise
                this.addTab = function (label, cssIcon, disable, active, params, callback) {
                    $scope.$settings.tabs.push({
                        label: $.trim(label) === '' ? 'No Label' : label,
                        cssIcon: cssIcon,
                        disable: disable,
                        active: active,
                        params: params || null,
                        callback: callback
                    });
                };


                // Set default tab
                $timeout(function () {
                    if ($scope.defaultIndex > ($scope.$settings.tabs.length - 1))
                        $scope.defaultIndex = 0;
                    $scope.activeTab($scope.defaultIndex);
                });
            }
        };
    }).directive('fwTab', function () {
        return {
            restrict: 'E',
            require: '^^fwTabs',
            transclude: true,
            replace: true,
            scope: {
                label: '@',
                cssIcon: '@',
                disable: '=?',
                active: '=?',
                params: '=?', // Parameter ที่ต้องการส่งกลับไปเมื่อมีการคลิก Tab
                callback: '&?' // callback(tabObject) and required to return promise
            },
            template: '<div id="fwTabItem"><div ng-transclude class="animated fadeIn"></div></div>',
            link: function (scope, attrs, element, fwTabs) {
                fwTabs.addTab(scope.label, scope.cssIcon, scope.disable, scope.active, scope.params, scope.callback);
            }
        };
    }).directive('fwSwipper', function () {
        // required:
        // swipper.min.js, swipper.min.css
        return {
            restrict: 'E',
            transclude: true,
            replace: true,
            template:
                '<div class="swiper-container">' +
                '   <div ng-transclude class="swiper-wrapper"></div>' +
                '   <div class="swiper-button-next"></div>' +
                '   <div class="swiper-button-prev"></div>' +
                '</div> ',
            controller: function ($timeout, $element) {
                $timeout(function () {
                    var swiper = new Swiper($element, {
                        //autoplay: { delay: 5000 },
                        speed: 900,
                        slidesPerView: 4,
                        spaceBetween: 5,
                        slidesPerGroup: 4,
                        loop: false,
                        loopFillGroupWithBlank: true,
                        pagination: {
                            el: '.swiper-pagination',
                            clickable: true,
                        },
                        navigation: {
                            nextEl: '.swiper-button-next',
                            prevEl: '.swiper-button-prev',
                        },
                    });
                });
            }
        };
    }).directive('fwSwipperItem', function () {
        return {
            restrict: 'E',
            require: '^^fwSwipper',
            transclude: true,
            replace: true,
            template: '<div ng-transclude class="swiper-slide"></div>'//,
            //link: function (scope, attrs, element, fwTabs) {
            //    fwTabs.addTab(scope.label, scope.cssIcon, scope.disable, scope.active, scope.params, scope.callback);
            //}
        };
    }).directive('fwBtnGroups', function () {
        return {
            restrict: 'E',
            replace: true,
            transclude: true,
            template:
                '<div id="{{uniqueId}}" class="btn-group btn-group-{{size}} shadow-sm {{css}}" ng-transclude></div>',
            scope: {
                size: '@', // lg,sm,(empty)
                color: '@', // primary, secondary, danger, warning, success, info, dark
                css: '@' // Additional css
            },
            controller: function ($scope, $filter) {
                $scope.uniqueId = $filter('textFormat')('fwBtnGroups_Id_{0}_{1}', new Date().getTime(), Math.floor(Math.random()));
                $scope.color = $.trim($scope.color) === '' ? 'primary' : $scope.color;

                this.getUniqueId = function () {
                    return $scope.uniqueId;
                };
                this.getBtnColor = function () {
                    return $scope.color;
                };
            }
        };
    }).directive('fwBtnGroupItem', function ($filter) {
        return {
            restrict: 'E',
            require: '^^fwBtnGroups',
            replace: true,
            template: '<button id="{{uniqueId}}" type="button" style="{{style}}" class="btn {{class}}" ng-disabled="disable" ng-click="fireClick($event)"><i ng-if="icon" class="{{icon}} mr-1"></i>{{label}}</button>',
            scope: {
                label: '@',
                icon: '@', // Css icon class
                color: '@', // Button color (primary, danger, orange, ...)
                style: '@',
                disable: '=?',
                active: '=?', // true/false
                click: '&?'
            },
            link: function ($scope, $element, $attrs, fwBtnGroups) {

                var btnColor = fwBtnGroups.getBtnColor();
                if ($.trim($scope.color) !== '')
                    btnColor = $scope.color;

                var activeClass = $filter('textFormat')('btn btn-{0} active', btnColor);
                var inactiveClass = $filter('textFormat')('btn btn-outline-{0}', btnColor);

                $scope.class = $scope.active ? activeClass : inactiveClass;
                $scope.uniqueId = $filter('textFormat')('{0}_fwBtnGroupItem_{1}_{2}', fwBtnGroups.getUniqueId(), new Date().getTime(), Math.floor(Math.random()));
                $scope.label = $.trim($scope.label) === '' ? 'No Label' : $scope.label;

                $scope.fireClick = function (event) {
                    var btnGroupContainer = $element.parent($filter('textFormat')('div#{0}', fwBtnGroups.getUniqueId()));
                    if (undefined !== $scope.click)
                        $scope.click();

                    btnGroupContainer.children('button').prop('class', inactiveClass);
                    $element.prop('class', activeClass);
                };
            }
        };
    }).filter('regexReplacer', function () {
        return function (str, pattern) {
            var regex = eval('/' + pattern + '/ig');
            return (str || '').replace(regex, '');
        };
    }).filter('sqlDate', function ($filter, $rootScope) {
        // แปลงเวลาของ SQL /Date(1588006800000)/	ให้เป็น Locale
        // showTime = true/false (Default: false) ต้องการแสดงเวลาด้วยหรือไม่
        // culture list => https://dotnetfiddle.net/e1BX7M
        return function (dateStr, format, locale, showTime) {
            if (null == dateStr || undefined == dateStr)
                return '';
            var regex = /^\/date\(|\)\/$/ig;
            var timestamp = dateStr.replace(regex, '');
            var date = new Date(+timestamp); // + is convert value to number

            // https://medium.com/swlh/use-tolocaledatestring-to-format-javascript-dates-2959108ea020
            var dateFormat = { year: 'numeric', month: '2-digit', day: '2-digit' };
            if (showTime)
                dateFormat = { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' };
            return date.toLocaleDateString(locale || $rootScope.defaultLocale, dateFormat);
        };
    }).filter('dateStrToLocale', function ($rootScope) {
        // แปลงเวลาที่เป็น String ที่อยู่ในรูปแบบ dd/MM/yyyy (01/02/2020) ให้เป็น Locale (Default: th-TH)
        return function (dateStr, locale) {
            dateStr = $.trim(dateStr || '');
            if ('' == dateStr)
                return '';

            var dateParts = dateStr.split('/');
            var date = new Date(+dateParts[2], (+dateParts[1]) - 1, +dateParts[0], 0, 0, 0, 0);
            // https://medium.com/swlh/use-tolocaledatestring-to-format-javascript-dates-2959108ea020
            return date.toLocaleDateString(locale || $rootScope.defaultLocale, { year: 'numeric', month: '2-digit', day: '2-digit' });
        };
    }).filter('convertYearToBuddhist', function ($fwDateService) {
        return function (strYear) {
            return $fwDateService.convertYearToBuddhist(strYear);
        }
    })
    .filter('displayDecimal', function ($filter) {
        // จัดการแสดงผลข้อมูล Decimal 
        // กรณีไม่มีค่าหลัง ทศนิยม ให้แสดงเป็นเลขจำนวนเต็ม
        return function (val, decAmt) {
            if (val == null || val == undefined)
                return '-';
            var newVal = '' + val;
            var parts = newVal.split('.');
            var decimal = +parts[1];
            return $filter('number')(val, decimal > 0 ? decAmt : 0);
        };
    }).filter('displayWorkingTime', function ($filter) {
        // จัดรูปแบบแสดงข้อมูลเวลาการทำงาน เช่น 0.28 => 28 นาที, 1.34 => 1 ชม. 34 นาที
        return function (val) {
            if (val == null || val == undefined)
                return 0;
            var newVal = (+val || 0).toFixed(2);
            var parts = newVal.split('.');

            var hours = +parts[0];
            var minutes = +parts[1];
            if (hours > 0)
                return $filter('textFormat')('{0} ชั่งโมง {1} นาที', hours, minutes);
            else
                return minutes == 0 || isNaN(minutes) ? '0 นาที' : $filter('textFormat')('{0} นาที', minutes);
        };
    }).filter('displayDays', function ($filter) {
        // จัดรูปแบบการแสดงผลวันที่ ให้อยู่ในรูปแบบที่ดูง่าย
        // เช่น 180 วัน = 6 เดือน
        // 365 วัน = 1 ปี
        return function (val) {
            val = +val; // แปลงค่าให้เป็นตัวเลข
            return $filter('number')(val, 0);
            //var years = Math.floor(val / 365.00);
            //var months = Math.floor(val / 30.00);
            //var days = val % 30;

            //var displayText = '';
            //if (val % 356)
            //    displayText = $filter('textFormat')('{0} ปี ', years);
            //if (val % 30)
            //    displayText = $filter('textFormat')('{0}{1} เดือน ', displayText, months);

            //displayText = $filter('textFormat')('{0}{1} วัน', displayText, days);

            //return displayText;
        };
    }).filter('seekArrayRange', function () {
        // สำหรับดึงข้อมูลจาก Array เพื่อนำไปแสดงผลตาม startIndex & endIndex
        // startIndex: ให้ผ่านค่าเริ่มต้นที่ 0
        return function (arrays, startIndex, endIndex) {
            arrays = arrays || [];
            return arrays.slice(startIndex, endIndex);
        };
    }).service('$fwModalHelperService', function ($fwModalService, $q, $customHttp, $filter, $rootScope) {
        var self = this;

        // แสดงรายการบุคลาการตามหน่วยงาน
        // templateUrl: เรียกเข้าไปที่ Helper/GetHelperPersonSearchForm
        // depId: รหัสหน่วยงานที่ต้องการ Default หลังจากที่แสดง Modal
        this.getPersonnelSelectOneModal = function (event, templateUrl, depId) {
            return $q(function (resolve) {
                $fwModalService.getModal(templateUrl, { $depId: depId }, function ($scope, $mdDialog, $depId, $q, $timeout) {
                    $scope.$settings = {
                        isLoading: false,
                        formSearch: {
                            personTypeId: null,
                            positionId: null,
                            depId: $depId,
                            personCode: '',
                            personName: ''
                        },
                        tableConfigs: {
                            columns: [
                                {
                                    label: '...',
                                    className: 'text-center word-wrap bg-light', type: 'html', params: 'BTN_SELECT',
                                    field: '<a href="javascript:void(0)" class="f-w-900 text-primary">เลือก</a>',
                                    style: 'min-width:70px;max-width:70px;width:70px;'
                                },
                                { label: 'รหัสพนักงาน', className: 'text-center word-wrap', type: 'field', field: 'PERSON_CODE', style: 'min-width:100px;max-width:100px;width:100px;' },
                                {
                                    label: 'ชื่อ - นามสกุล', className: 'text-left word-wrap bg-light f-w-900', type: 'html', params: 'BTN_SELECT',
                                    field: '<a href="javascript:void(0)"><span class="text-primary">{{row.FIRST_NAME}} {{row.LAST_NAME}}</span></a><div class="text-danger f-12 f-w-900" ng-if="null!=row.LAST_LOGIN_DATETIME">[เข้าสู่ระบบครั้งล่าสุด: {{row.LAST_LOGIN_DATETIME|sqlDate:\'dd/MM/yyyy HH:mm:ss\'}}]</div>', style: 'min-width:180px;'
                                },
                                { label: 'หน่วยงาน', className: 'text-center word-wrap', type: 'field', field: 'DEP_NAME', style: 'min-width:150px;max-width:150px;width:150px;' },
                                { label: 'ตำแหน่งงาน', className: 'text-left word-wrap', type: 'field', field: 'POSITION_NAME', style: 'min-width:150px;max-width:150px;width:150px;' },
                                { label: 'ประเภทบุคลากร', className: 'text-left word-wrap', type: 'field', field: 'PERSON_TYPE_NAME', style: 'min-width:150px;max-width:150px;width:150px;' }
                            ]
                        }
                    };

                    // ส่งข้อมูลไปค้นหา
                    var submitSearchId = null;
                    $scope.submitSearch = function () {
                        $timeout.cancel(submitSearchId);
                        submitSearchId = $timeout(function () {
                            $scope.$broadcast('fwSimpleDataTable.modal.paramsChanged', {
                                personTypeId: $scope.$settings.formSearch.personTypeId,
                                positionId: $scope.$settings.formSearch.positionId,
                                depId: $scope.$settings.formSearch.depId,
                                personCode: $scope.$settings.formSearch.personCode,
                                personName: $scope.$settings.formSearch.personName
                            })
                        }, 900);
                    };
                    // เมื่อมีการคลิกที่ แถวของข้อมูล
                    $scope.rowClick = function (row, columnConfig) {
                        if (columnConfig.params == 'BTN_SELECT')
                            $mdDialog.hide(row);

                        return $q(function (resolve) {
                            resolve({ row: row });
                        });
                    }
                    // Close Modal
                    $scope.close = function () {
                        $mdDialog.hide(null);
                    };

                    // กำหนดค่าพื้นฐาน
                    $scope.submitSearch();
                }, event).then(function (res) {
                    resolve(res);
                });
            });
        };


        // แสดงรายการบุคลาการตามหน่วยงาน
        // templateUrl: เรียกเข้าไปที่ Helper/GetHelperPersonSearchMultiSelectForm
        // persons: รายการบุคลากรที่เลือกไว้แล้ว (PERSON_ID, PERSON_NAME)
        // depId: Default เลือกหน่วยงานใด
        this.getPersonnelSelectMultipleModal = function (event, templateUrl, persons, depId) {
            return $q(function (resolve) {
                $fwModalService.getModal(templateUrl, { $persons: persons || [], $depId: depId || 'empty' }, function ($scope, $mdDialog, $persons, $depId, $q, $timeout) {
                    $scope.$settings = {
                        isLoading: false,
                        selectItems: [],
                        formSearch: {
                            personTypeId: 'empty',
                            positionId: 'empty',
                            periodId: 'empty',
                            depId: $depId,
                            personCode: '',
                            personName: ''
                        },
                        tableConfigs: {
                            columns: [
                                {
                                    label: '<input type="checkbox" />',
                                    className: 'text-center word-wrap bg-light', type: 'html', params: 'CHECKBOX',
                                    field: '<input type="checkbox" ng-model="row.checked" ng-disabled="disabled" />',
                                    style: 'min-width:70px;max-width:70px;width:70px;'
                                },
                                { label: 'รหัสพนักงาน', className: 'text-center word-wrap', type: 'field', field: 'PERSON_CODE', style: 'min-width:100px;max-width:100px;width:100px;' },
                                {
                                    label: 'ชื่อ - นามสกุล', className: 'text-left word-wrap bg-light f-w-900', type: 'html', params: 'BTN_SELECT',
                                    field: '<a href="javascript:void(0)"><span class="text-primary">{{row.FIRST_NAME}} {{row.LAST_NAME}}</span></a><div class="text-danger f-12 f-w-900" ng-if="null!=row.LAST_LOGIN_DATETIME">[เข้าสู่ระบบครั้งล่าสุด: {{row.LAST_LOGIN_DATETIME|sqlDate:\'dd/MM/yyyy HH:mm:ss\'}}]</div>', style: 'min-width:180px;'
                                },
                                { label: 'หน่วยงาน', className: 'text-center word-wrap', type: 'field', field: 'DEP_NAME', style: 'min-width:150px;max-width:150px;width:150px;' },
                                { label: 'ตำแหน่งงาน', className: 'text-left word-wrap', type: 'field', field: 'POSITION_NAME', style: 'min-width:150px;max-width:150px;width:150px;' },
                                { label: 'ประเภทบุคลากร', className: 'text-left word-wrap', type: 'field', field: 'PERSON_TYPE_NAME', style: 'min-width:150px;max-width:150px;width:150px;' },
                            ]
                        }
                    };

                    // ส่งข้อมูลไปค้นหา
                    var submitSearchId = null;
                    $scope.submitSearch = function () {
                        $timeout.cancel(submitSearchId);
                        submitSearchId = $timeout(function () {
                            $scope.$broadcast('fwSimpleDataTable.person.modal.paramsChanged', {
                                personTypeId: ($scope.$settings.formSearch.personTypeId || '').replace('empty', ''),
                                positionId: ($scope.$settings.formSearch.positionId || '').replace('empty', ''),
                                periodId: ($scope.$settings.formSearch.periodId || '').replace('empty', ''),
                                depId: $scope.$settings.formSearch.depId,
                                personCode: $scope.$settings.formSearch.personCode,
                                personName: $scope.$settings.formSearch.personName
                            })
                        }, 300);
                    };
                    // เมื่อ รหัส และ ชื่อ-นามสกุล เปลี่ยนแปลง
                    var personChangedId = null;
                    $scope.personChanged = function () {
                        $timeout.cancel(personChangedId);
                        personChangedId = $timeout(function () {
                            $scope.submitSearch();
                        }, 1000);
                    };
                    // เมื่อค้นหาเสร็จ
                    $scope.searchDone = function (res) {
                        res.data.rows = res.data.rows || [];
                        $.each(res.data.rows, function (index, row) {
                            res.data.rows[index] = $.extend(true, {
                                checked: $scope.$settings.selectItems.filter(function (item) {
                                    return item.PERSON_ID == row.PERSON_ID;
                                }).length > 0
                            }, row);
                        });

                        return $q(function (resolve) {
                            resolve(res);
                        });
                    };
                    // เมื่อคลิกที่คอลัมล์
                    $scope.columnClick = function (event, rows, columnConfig) {
                        if (columnConfig.params == 'CHECKBOX') {
                            var checked = event.target.checked;
                            if (undefined != checked) {
                                $.each(rows, function (index, row) {
                                    var personIds = $scope.$settings.selectItems.map(function (item) { return item.PERSON_ID; });
                                    var foundIndex = personIds.indexOf(row.PERSON_ID);
                                    if (foundIndex == -1 && checked)
                                        $scope.$settings.selectItems.push(row);
                                    else if (foundIndex != -1 && !checked)
                                        $scope.$settings.selectItems.splice(foundIndex, 1);

                                    row.checked = checked;
                                });
                            }
                        }

                        return $q(function (resolve) {
                            resolve({ rows: rows, columnConfig: columnConfig });
                        });
                    };
                    // เมื่อมีการคลิกที่ แถวของข้อมูล
                    $scope.rowClick = function (event, row, columnConfig) {
                        if (columnConfig.params == 'CHECKBOX') {
                            var checked = event.target.checked;
                            if (checked != undefined) {
                                var personIds = $scope.$settings.selectItems.map(function (item) { return item.PERSON_ID; });
                                var foundIndex = personIds.indexOf(row.PERSON_ID);
                                if (foundIndex == -1 && checked)
                                    $scope.$settings.selectItems.push(row);
                                else if (foundIndex != -1 && !checked)
                                    $scope.$settings.selectItems.splice(foundIndex, 1);
                            }
                        }

                        return $q(function (resolve) {
                            resolve({ row: row });
                        });
                    }
                    // Close Modal
                    $scope.close = function () {
                        $mdDialog.cancel(null);
                    };
                    $scope.ok = function () {
                        $mdDialog.hide($scope.$settings.selectItems);
                    };

                    // กำหนดค่าพื้นฐาน
                    $scope.$settings.selectItems = $scope.$settings.selectItems.concat($persons);
                    $scope.submitSearch();
                }, event).then(function (res) {
                    resolve(res);
                });
            });
        };



        // แสดงรายการบุคลาการตามหน่วยงาน
        // templateUrl: เรียกเข้าไปที่ Helper/GetHelperDepartmentSearchMultiSelectForm
        // departments: รายการหน่วยงานที่เลือกไว้แล้ว (DEP_ID, DEP_NAME)
        this.getDepartmentSelectMultiModal = function (event, departments) {
            var templateUrl = $rootScope.baseUrl + '/Helper/GetHelperDepartmentSearchMultiSelectForm';
            return $q(function (resolve) {
                $fwModalService.getModal(templateUrl, { $departments: departments || [] }, function ($scope, $mdDialog, $departments, $q, $timeout) {
                    $scope.$settings = {
                        isLoading: false,
                        selectItems: [],
                        formSearch: {
                            depName: ''
                        },
                        tableConfigs: {
                            columns: [
                                {
                                    label: '<input type="checkbox" />',
                                    className: 'text-center word-wrap bg-light', type: 'html', params: 'CHECKBOX',
                                    field: '<input type="checkbox" ng-model="row.checked" ng-disabled="disabled" />',
                                    style: 'min-width:70px;max-width:70px;width:70px;'
                                },
                                { label: 'รหัสหน่วยงาน', className: 'text-center word-wrap', type: 'field', field: 'DEP_ID', style: 'min-width:150px;max-width:150px;width:150px;' },
                                { label: 'ชื่อหน่วยงาน', className: 'text-left word-wrap', type: 'field', field: 'DEP_NAME', style: 'min-width:150px;' }
                            ]
                        }
                    };

                    // ส่งข้อมูลไปค้นหา
                    var submitSearchId = null;
                    $scope.submitSearch = function () {
                        $timeout.cancel(submitSearchId);
                        submitSearchId = $timeout(function () {
                            $scope.$broadcast('fwSimpleDataTable.departmentSelectMulti.modal.paramsChanged', {
                                depName: $scope.$settings.formSearch.depName
                            })
                        }, 300);
                    };
                    // เมื่อชื่อหน่วยงานเปลี่ยนแปลง
                    var depNameChangedId = null;
                    $scope.depNameChanged = function () {
                        $timeout.cancel(depNameChangedId);
                        depNameChangedId = $timeout(function () {
                            $scope.submitSearch();
                        }, 700);
                    };
                    // เมื่อค้นหาเสร็จ
                    $scope.searchDone = function (res) {
                        res.data.rows = res.data.rows || [];
                        $.each(res.data.rows, function (index, row) {
                            res.data.rows[index] = $.extend(true, {
                                checked: $scope.$settings.selectItems.filter(function (item) {
                                    return item.DEP_ID == row.DEP_ID;
                                }).length > 0
                            }, row);
                        });

                        return $q(function (resolve) {
                            resolve(res);
                        });
                    };
                    // เมื่อคลิกที่คอลัมล์
                    $scope.columnClick = function (event, rows, columnConfig) {
                        if (columnConfig.params == 'CHECKBOX') {
                            var checked = event.target.checked;
                            if (undefined != checked) {
                                $.each(rows, function (index, row) {
                                    var depIds = $scope.$settings.selectItems.map(function (item) { return item.DEP_ID; });
                                    var foundIndex = depIds.indexOf(row.DEP_ID);
                                    if (foundIndex == -1 && checked)
                                        $scope.$settings.selectItems.push(row);
                                    else if (foundIndex != -1 && !checked)
                                        $scope.$settings.selectItems.splice(foundIndex, 1);

                                    row.checked = checked;
                                });
                            }
                        }

                        return $q(function (resolve) {
                            resolve({ rows: rows, columnConfig: columnConfig });
                        });
                    };
                    // เมื่อมีการคลิกที่ แถวของข้อมูล
                    $scope.rowClick = function (event, row, columnConfig) {
                        if (columnConfig.params == 'CHECKBOX') {
                            var checked = event.target.checked;
                            if (checked != undefined) {
                                var depIds = $scope.$settings.selectItems.map(function (item) { return item.DEP_ID; });
                                var foundIndex = depIds.indexOf(row.DEP_ID);
                                if (foundIndex == -1 && checked)
                                    $scope.$settings.selectItems.push(row);
                                else if (foundIndex != -1 && !checked)
                                    $scope.$settings.selectItems.splice(foundIndex, 1);
                            }
                        }

                        return $q(function (resolve) {
                            resolve({ row: row });
                        });
                    }
                    // Close Modal
                    $scope.close = function () {
                        $mdDialog.cancel(null);
                    };
                    $scope.ok = function () {
                        $mdDialog.hide($scope.$settings.selectItems);
                    };

                    // กำหนดค่าพื้นฐาน
                    $scope.$settings.selectItems = $scope.$settings.selectItems.concat($departments);
                    $scope.submitSearch();
                }, event).then(function (res) {
                    resolve(res);
                });
            });
        };

        // แสดงข้อมูลรายการค่าใช้จ่าย
        // required: select2
        this.getExpensesSelectMultiModal = function (event, selectedExpenses) {
            var templateUrl = $rootScope.baseUrl + '/Helper/GetHelperExpensesSearchMultiSelectForm';
            return $q(function (resolve) {
                $fwModalService.getModal(templateUrl, { $selectedExpenses: selectedExpenses || [] }, function ($scope, $mdDialog, $selectedExpenses, $q, $timeout, $customHttp) {
                    $scope.$settings = {
                        isLoading: false,
                        selectItems: [],
                        expensesGroups: [],
                        formSearch: {
                            expensesGroupId: 'empty',
                            expensesName: ''
                        },
                        tableConfigs: {
                            columns: [
                                {
                                    label: '<input type="checkbox" />',
                                    className: 'text-center word-wrap bg-light', type: 'html', params: 'CHECKBOX',
                                    field: '<input type="checkbox" ng-model="row.checked" ng-disabled="disabled" />',
                                    style: 'min-width:70px;max-width:70px;width:70px;'
                                },
                                { label: 'รหัส', className: 'text-center word-wrap', type: 'field', field: 'EXPENSES_ID', style: 'min-width:80px;max-width:80px;width:80px;' },
                                { label: 'รายการค่าใช้จ่าย', className: 'text-left word-wrap', type: 'field', field: 'EXPENSES_NAME', style: 'min-width:200px;max-width:200px;width:200px;' },
                                { label: 'หมวดค่าใช้จ่าย', className: 'text-left word-wrap', type: 'field', field: 'EXPENSES_GROUP_NAME', style: 'min-width:150px;' }
                            ]
                        }
                    };

                    // โหลดข้อมูลหมวดค่าใช้จ่าย
                    $customHttp.formPost($scope.$root.baseUrl + '/Helper/RetrieveAllExpensesGroup', {}).then(function (res) {
                        $scope.$settings.expensesGroups = res.data || [];
                    }, function () { });
                    // ส่งข้อมูลไปค้นหา
                    var submitSearchId = null;
                    $scope.submitSearch = function () {
                        $timeout.cancel(submitSearchId);
                        submitSearchId = $timeout(function () {
                            $scope.$broadcast('fwSimpleDataTable.expenses.modal.paramsChanged', {
                                expensesGroupId: ('' + $scope.$settings.formSearch.expensesGroupId).replace(/[^\d]/g, ''),
                                expensesName: $scope.$settings.formSearch.expensesName
                            });
                        }, 300);
                    };
                    // เมื่อชื่อหน่วยงานเปลี่ยนแปลง
                    var expensesNameChangedId = null;
                    $scope.expensesNameChanged = function () {
                        $timeout.cancel(expensesNameChangedId);
                        expensesNameChangedId = $timeout(function () {
                            $scope.submitSearch();
                        }, 500);
                    };
                    // เมื่อค้นหาเสร็จ
                    $scope.searchDone = function (res) {
                        res.data.rows = res.data.rows || [];
                        $.each(res.data.rows, function (index, row) {
                            res.data.rows[index] = $.extend(true, {
                                checked: $scope.$settings.selectItems.filter(function (item) {
                                    return item.EXPENSES_ID == row.EXPENSES_ID;
                                }).length > 0
                            }, row);
                        });

                        return $q(function (resolve) {
                            resolve(res);
                        });
                    };
                    // เมื่อคลิกที่คอลัมล์
                    $scope.columnClick = function (event, rows, columnConfig) {
                        if (columnConfig.params == 'CHECKBOX') {
                            var checked = event.target.checked;
                            if (undefined != checked) {
                                var expensesIds = $scope.$settings.selectItems.map(function (item) { return item.EXPENSES_ID; });
                                $.each(rows, function (index, row) {
                                    var foundIndex = expensesIds.indexOf(row.EXPENSES_ID);
                                    if (foundIndex == -1 && checked)
                                        $scope.$settings.selectItems.push(row);
                                    else if (foundIndex != -1 && !checked)
                                        $scope.$settings.selectItems.splice(foundIndex, 1);

                                    row.checked = checked;
                                });
                            }
                        }

                        return $q(function (resolve) {
                            resolve({ rows: rows, columnConfig: columnConfig });
                        });
                    };
                    // เมื่อมีการคลิกที่ แถวของข้อมูล
                    $scope.rowClick = function (event, row, columnConfig) {
                        if (columnConfig.params == 'CHECKBOX') {
                            var checked = event.target.checked;
                            if (checked != undefined) {
                                var expensesIds = $scope.$settings.selectItems.map(function (item) { return item.EXPENSES_ID; });
                                var foundIndex = expensesIds.indexOf(row.EXPENSES_ID);
                                if (foundIndex == -1 && checked)
                                    $scope.$settings.selectItems.push(row);
                                else if (foundIndex != -1 && !checked)
                                    $scope.$settings.selectItems.splice(foundIndex, 1);
                            }
                        }

                        return $q(function (resolve) {
                            resolve({ row: row });
                        });
                    }
                    // Close Modal
                    $scope.close = function () {
                        $mdDialog.cancel(null);
                    };
                    $scope.ok = function () {
                        $mdDialog.hide($scope.$settings.selectItems);
                    };

                    // กำหนดค่าพื้นฐาน
                    $scope.$settings.selectItems = $scope.$settings.selectItems.concat($selectedExpenses);
                    $scope.submitSearch();
                }, event).then(function (res) {
                    resolve(res);
                });
            });
        };


        this.getExpensesSelectOneModal = function (event, defaultExpensesGroupId, expensesGroupReadonly) {
            var templateUrl = $rootScope.baseUrl + '/Helper/GetHelperExpensesSearchOneSelectForm';
            defaultExpensesGroupId = '' + (defaultExpensesGroupId || 'empty');
            expensesGroupReadonly = angular.isUndefined(expensesGroupReadonly) ? false : expensesGroupReadonly;
            return $q(function (resolve) {
                $fwModalService.getModal(templateUrl, { $defaultExpensesGroupId: defaultExpensesGroupId, $expensesGroupReadonly: expensesGroupReadonly }, function ($scope, $customHttp, $defaultExpensesGroupId, $expensesGroupReadonly, $mdDialog, $q, $timeout) {
                    $scope.$settings = {
                        isLoading: false, expensesGroupReadonly: $expensesGroupReadonly,
                        expensesGroups: [],
                        formSearch: {
                            expensesGroupId: $defaultExpensesGroupId,
                            expensesName: ''
                        },
                        tableConfigs: {
                            columns: [
                                {
                                    label: '',
                                    className: 'text-center word-wrap bg-light', type: 'html', params: 'BTN_SELECT',
                                    field: '<a href="javascript:void(0)" class="f-w-900 text-primary">เลือก</a>',
                                    style: 'min-width:70px;max-width:70px;width:70px;'
                                },
                                { label: 'รหัส', className: 'text-center word-wrap', type: 'field', field: 'EXPENSES_ID', style: 'min-width:80px;max-width:80px;width:80px;' },
                                { label: 'รายการค่าใช้จ่าย', className: 'text-left word-wrap', type: 'field', field: 'EXPENSES_NAME', style: 'min-width:200px;max-width:200px;width:200px;' },
                                { label: 'หมวดค่าใช้จ่าย', className: 'text-left word-wrap', type: 'field', field: 'EXPENSES_GROUP_NAME', style: 'min-width:150px;' }
                            ]
                        }
                    };

                    // โหลดข้อมูลหมวดค่าใช้จ่าย
                    $customHttp.formPost($scope.$root.baseUrl + '/Helper/RetrieveAllExpensesGroup', {}).then(function (res) {
                        $scope.$settings.expensesGroups = res.data || [];
                    }, function () { });
                    // เมื่อชื่อหน่วยงานเปลี่ยนแปลง
                    var expensesNameChangedId = null;
                    $scope.expensesNameChanged = function () {
                        $timeout.cancel(expensesNameChangedId);
                        expensesNameChangedId = $timeout(function () {
                            $scope.submitSearch();
                        }, 500);
                    };
                    // ส่งข้อมูลไปค้นหา
                    var submitSearchId = null;
                    $scope.submitSearch = function () {
                        $timeout.cancel(submitSearchId);
                        submitSearchId = $timeout(function () {
                            $scope.$broadcast('fwSimpleDataTable.expenses.modal.paramsChanged', {
                                expensesGroupId: ('' + $scope.$settings.formSearch.expensesGroupId).replace(/[^\d]/g, ''),
                                expensesName: $scope.$settings.formSearch.expensesName
                            });
                        }, 300);
                    };
                    // เมื่อมีการคลิกที่ แถวของข้อมูล
                    $scope.rowClick = function (row, columnConfig) {
                        if (columnConfig.params == 'BTN_SELECT')
                            $mdDialog.hide(row);

                        return $q(function (resolve) {
                            resolve({ row: row });
                        });
                    }
                    // Close Modal
                    $scope.close = function () {
                        $mdDialog.hide(null);
                    };

                    // กำหนดค่าพื้นฐาน
                    $scope.submitSearch();
                }, event).then(function (res) {
                    resolve(res);
                });
            });
        };



        // แสดงข้อมูลใบกันเงิน
        // templateUrl: เรียกเข้าไปที่ Helper/GetHelperReserveSearchSelectOneForm
        // fiscalYear: ปีที่กันเงิน, subDepId: หน่วยงานภายใน ที่กันเงิน
        this.getReserveBudgetSelectOneModal = function (event, fiscalYear, subDepId) {
            var templateUrl = $rootScope.baseUrl + '/Helper/GetHelperReserveBudgetSearchSelectOneForm';
            return $q(function (resolve) {
                $fwModalService.getModal(templateUrl, { $fiscalYear: fiscalYear, $subDepId: subDepId }, function ($scope, $mdDialog, $fiscalYear, $subDepId, $q, $timeout) {
                    $scope.$settings = {
                        isLoading: false,
                        expensesGroups: [], expenses: [],
                        formSearch: {
                            fiscalYear: $fiscalYear || null,
                            subDepId: $subDepId || 'empty',
                            planId: 'empty', produceId: 'empty', activityId: 'empty',
                            budgetTypeId: 'empty', expensesGroupId: 'empty', expensesId: 'empty',
                            budgetType: '', reserveType: ''
                        },
                        tableConfigs: {
                            columns: [
                                {
                                    label: '...',
                                    className: 'text-center word-wrap bg-light', type: 'html', params: 'BTN_SELECT',
                                    field: '<a href="javascript:void(0)" class="f-w-900 text-primary">เลือก</a>',
                                    style: 'min-width:70px;max-width:70px;width:70px;'
                                },
                                { label: 'เลขที่กันเงิน', className: 'text-center word-wrap', type: 'field', field: 'RESERVE_ID', style: 'min-width:120px;max-width:120px;width:120px;' },
                                { label: 'หน่วยงานภายใน', className: 'text-left word-wrap', type: 'field', field: 'SUB_DEP_NAME', style: 'min-width:190px;max-width:190px;width:190px;' },
                                { label: 'ผู้ทำรายการ', className: 'text-left word-wrap', type: 'field', field: 'RESERVE_NAME', style: 'width:145px;min-width:145px;max-width:145px;' },
                                { label: 'วันที่ทำรายการ', className: 'text-center word-wrap', type: 'expression', field: '{{row.CREATED_DATETIME|sqlDate:\'\':null:true}}', style: 'min-width:130px;max-width:130px;width:130px;' },
                                { label: 'กันเงิน (บาท)', className: 'text-right', type: 'fieldNumber', field: 'RESERVE_BUDGET_AMOUNT', style: 'min-width:140px;max-width:140px;width:140px;' },
                                { label: 'เบิกจ่าย (บาท)', className: 'text-right', type: 'fieldNumber', field: 'USE_AMOUNT', style: 'min-width:140px;max-width:140px;width:140px;' },
                                { label: 'คงเหลือ (บาท)', className: 'text-right', type: 'fieldNumber', field: 'REMAIN_AMOUNT', style: 'min-width:140px;max-width:140px;width:140px;' }
                            ]
                        }
                    };

                    // ส่งข้อมูลไปค้นหา
                    var submitSearchId = null;
                    $scope.submitSearch = function () {
                        $timeout.cancel(submitSearchId);
                        submitSearchId = $timeout(function () {
                            $scope.$broadcast('fwSimpleDataTable.modal.paramsChanged', {
                                fiscalYear: $fiscalYear || null,
                                subDepId: $subDepId || null,
                                planId: ('' + $scope.$settings.formSearch.planId).replace('empty', ''),
                                produceId: ('' + $scope.$settings.formSearch.produceId).replace('empty', ''),
                                activityId: ('' + $scope.$settings.formSearch.activityId).replace('empty', ''),
                                budgetTypeId: ('' + $scope.$settings.formSearch.budgetTypeId).replace('empty', ''),
                                expensesGroupId: ('' + $scope.$settings.formSearch.expensesGroupId).replace('empty', ''),
                                expensesId: ('' + $scope.$settings.formSearch.expensesId).replace('empty', ''),
                                budgetType: $scope.$settings.formSearch.budgetType,
                                reserveType: $scope.$settings.formSearch.reserveType,
                            })
                        }, 300);
                    };
                    // เมื่องบรายจ่าย เปลี่ยนแปลง
                    var budgetTypeIdChangedId = null;
                    $scope.budgetTypeChanged = function () {
                        $timeout.cancel(budgetTypeIdChangedId);
                        budgetTypeIdChangedId = $timeout(function () {
                            $scope.$settings.expensesGroups = [];
                            $scope.$settings.expenses = [];
                            $scope.$settings.formSearch.expensesGroupId = 'empty';
                            $scope.$settings.formSearch.expensesId = 'empty';
                            var budgetTypeId = ('' + $scope.$settings.formSearch.budgetTypeId).replace('empty', '');
                            $customHttp.formPost($rootScope.baseUrl + '/Helper/RetrieveExpensesGroupByBudgetType', { budgetTypeId: budgetTypeId }).then(function (res) {
                                $scope.$settings.expensesGroups = res.data || [];
                                $timeout(function () {
                                    $scope.submitSearch();
                                }, 100);
                            }, function () { });
                        }, 300);
                    };
                    // เมื่อหมวดค่าใช้จ่าย เปลี่ยนแปลง
                    var expensesGroupIdChangedId = null;
                    $scope.expensesGroupChanged = function () {
                        $timeout.cancel(expensesGroupIdChangedId);
                        expensesGroupIdChangedId = $timeout(function () {
                            $scope.$settings.expenses = [];
                            $scope.$settings.formSearch.expensesId = 'empty';
                            var expensesGroupId = ('' + $scope.$settings.formSearch.expensesGroupId).replace('empty', '');
                            $customHttp.formPost($rootScope.baseUrl + '/Helper/RetrieveExpensesByExpensesGroup', { expensesGroupId: expensesGroupId }).then(function (res) {
                                $scope.$settings.expenses = res.data || [];
                                $timeout(function () {
                                    $scope.submitSearch();
                                }, 100);
                            }, function () { });
                        }, 300);
                    };
                    // เมื่อมีการคลิกที่ แถวของข้อมูล
                    $scope.rowClick = function (row, columnConfig) {
                        if (columnConfig.params == 'BTN_SELECT')
                            $mdDialog.hide(row);

                        return $q(function (resolve) {
                            resolve({ row: row });
                        });
                    }
                    // Close Modal
                    $scope.close = function () {
                        $mdDialog.hide(null);
                    };

                    // กำหนดค่าพื้นฐาน
                    $scope.submitSearch();
                }, event).then(function (res) {
                    resolve(res);
                });
            });
        };


        // แสดงรายการเมนูในระบบ
        this.getMenuSelectMultipleModal = function (event, templateUrl, menus) {
            return $q(function (resolve) {
                $fwModalService.getModal(templateUrl, { $menus: menus || [] }, function ($scope, $mdDialog, $menus, $q, $timeout) {
                    $scope.$settings = {
                        isLoading: false,
                        selectItems: [],
                        formSearch: {
                            menuName: ''
                        },
                        tableConfigs: {
                            columns: [
                                {
                                    label: '<input type="checkbox" />',
                                    className: 'text-center word-wrap bg-light', type: 'html', params: 'CHECKBOX',
                                    field: '<input type="checkbox" ng-model="row.checked" ng-disabled="disabled" />',
                                    style: 'min-width:70px;max-width:70px;width:70px;'
                                },
                                { label: 'No.', className: 'text-center word-wrap', type: 'field', field: 'MENU_ID', style: 'min-width:100px;max-width:100px;width:100px;' },
                                { label: 'ชื่อเมนู', className: 'text-left word-wrap bg-light f-w-900', type: 'field', field: 'MENU_NAME', style: 'min-width:180px;' }
                            ]
                        }
                    };

                    // ส่งข้อมูลไปค้นหา
                    var submitSearchId = null;
                    $scope.submitSearch = function () {
                        $timeout.cancel(submitSearchId);
                        submitSearchId = $timeout(function () {
                            $scope.$broadcast('fwSimpleDataTable.menu.modal.paramsChanged', {
                                menuName: $scope.$settings.formSearch.menuName
                            })
                        }, 300);
                    };
                    // เมื่อ ชื่อเมนูเปลี่ยนแปลง
                    var menuChangedId = null;
                    $scope.menuChanged = function () {
                        $timeout.cancel(menuChangedId);
                        menuChangedId = $timeout(function () {
                            $scope.submitSearch();
                        }, 1000);
                    };
                    // เมื่อค้นหาเสร็จ
                    $scope.searchDone = function (res) {
                        res.data.rows = res.data.rows || [];
                        $.each(res.data.rows, function (index, row) {
                            res.data.rows[index] = $.extend(true, {
                                checked: $scope.$settings.selectItems.filter(function (item) {
                                    return item.MENU_ID == row.MENU_ID;
                                }).length > 0
                            }, row);
                        });

                        return $q(function (resolve) {
                            resolve(res);
                        });
                    };
                    // เมื่อคลิกที่คอลัมล์
                    $scope.columnClick = function (event, rows, columnConfig) {
                        if (columnConfig.params == 'CHECKBOX') {
                            var checked = event.target.checked;
                            if (undefined != checked) {
                                $.each(rows, function (index, row) {
                                    var menuIds = $scope.$settings.selectItems.map(function (item) { return item.MENU_ID; });
                                    var foundIndex = menuIds.indexOf(row.MENU_ID);
                                    if (foundIndex == -1 && checked)
                                        $scope.$settings.selectItems.push(row);
                                    else if (foundIndex != -1 && !checked)
                                        $scope.$settings.selectItems.splice(foundIndex, 1);

                                    row.checked = checked;
                                });
                            }
                        }

                        return $q(function (resolve) {
                            resolve({ rows: rows, columnConfig: columnConfig });
                        });
                    };
                    // เมื่อมีการคลิกที่ แถวของข้อมูล
                    $scope.rowClick = function (event, row, columnConfig) {
                        if (columnConfig.params == 'CHECKBOX') {
                            var checked = event.target.checked;
                            if (checked != undefined) {
                                var menuIds = $scope.$settings.selectItems.map(function (item) { return item.MENU_ID; });
                                var foundIndex = menuIds.indexOf(row.MENU_ID);
                                if (foundIndex == -1 && checked)
                                    $scope.$settings.selectItems.push(row);
                                else if (foundIndex != -1 && !checked)
                                    $scope.$settings.selectItems.splice(foundIndex, 1);
                            }
                        }

                        return $q(function (resolve) {
                            resolve({ row: row });
                        });
                    }
                    // เมื่อชื่อของเมนู เปลี่ยนแปลง
                    var menuNameChangedId = null
                    $scope.menuNameChanged = function () {
                        $timeout.cancel(menuNameChangedId);
                        menuNameChangedId = $timeout(function () {
                            $scope.submitSearch();
                        }, 300);
                    };
                    // Close Modal
                    $scope.close = function () {
                        $mdDialog.cancel(null);
                    };
                    $scope.ok = function () {
                        $mdDialog.hide($scope.$settings.selectItems);
                    };

                    // กำหนดค่าพื้นฐาน
                    $scope.$settings.selectItems = $scope.$settings.selectItems.concat($menus);
                    $scope.submitSearch();
                }, event).then(function (res) {
                    resolve(res);
                });
            });
        };


        // แสดงรายการ Template คำของบประมาณ
        // Required Components: select2
        // TemplateUrl: ให้เรียกไปที่ Helper/GetHelperBudgetTemplate
        // budgetSourceTypeFlag: 1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ
        // budgetSourceTypeFlagReadonly: true/false
        // budgetTargetFlag: 1 = งบประมาณตอนต้นปี, 2 = ของบเพิ่มเติม
        // budgetTargetFlagReadonly: true/false
        this.getBudgetTemplateSelectOnce = function (event, budgetTypeFlag, budgetTargetFlag) {
            var templateUrl = $rootScope.baseUrl + '/Helper/GetHelperSearchBudgetTemplate';
            return $q(function (resolve) {
                $fwModalService.getModal(templateUrl, {
                    $params: {
                        budgetTypeFlag: budgetTypeFlag || null,
                        budgetTargetFlag: budgetTargetFlag || null
                    }
                }, function ($scope, $mdDialog, $q, $timeout, $params) {
                    $scope.$settings = {
                        isLoading: false, isLoadExpensesGroup: false,
                        expensesGroups: [],
                        formSearch: {
                            templateName: '',
                            budgetTypeFlag: $params.budgetTypeFlag || '1', // 1 = เงิน งปม., 2 = เงินนอก งปม.
                            budgetTargetFlag: $params.budgetTargetFlag || '1', // 1 = งบประมาณตอนต้นปี, 2 = ของบเพิ่มเติม
                            planId: 'empty', produceId: 'empty', activityId: 'empty',
                            budgetTypeId: 'empty', expensesGroupId: 'empty'
                        },
                        tableConfigs: {
                            columns: [
                                {
                                    label: '',
                                    className: 'text-center word-wrap', type: 'html', params: 'BTN_SELECT',
                                    field: '<a href="javascript:void(0)" class="f-w-900 text-primary">เลือก</a>',
                                    style: 'min-width:70px;max-width:70px;width:70px;'
                                },
                                { label: 'ชื่อ Template', className: 'text-left word-wrap bg-light', type: 'field', field: 'TEMPLATE_NAME', style: 'min-width:200px;' },
                                { label: 'แผนงาน', className: 'text-left word-wrap', type: 'field', field: 'PLAN_NAME', style: 'width:176px;max-width:176px;min-width:176px;' },
                                { label: 'ผลผลิต', className: 'text-left word-wrap', type: 'field', field: 'PRODUCE_NAME', style: 'width:176px;max-width:176px;min-width:176px;' },
                                { label: 'กิจกรรม', className: 'text-left word-wrap', type: 'field', field: 'ACTIVITY_NAME', style: 'width:176px;max-width:176px;min-width:176px;' },
                                { label: 'งบรายจ่าย', className: 'text-left word-wrap', type: 'field', field: 'BUDGET_TYPE_NAME', style: 'width:176px;max-width:176px;min-width:176px;' },
                                { label: 'หมวด คชจ.', className: 'text-left word-wrap', type: 'field', field: 'EXPENSES_GROUP_NAME', style: 'width:150px;max-width:150px;min-width:150px;' },
                                {
                                    label: 'ใช้กับหน่วยงาน', className: 'text-center word-wrap', type: 'html',
                                    field: '<span ng-if="row.SHARED_DEP_TEMPLATE==1">ทุกหน่วยงาน</span>' +
                                        '<span class="text-danger" ng-if="row.SHARED_DEP_TEMPLATE==2">บางหน่วยงาน</span>', style: 'width:100px;max-width:100px;min-width:100px;'
                                },
                                {
                                    label: 'ใช้กับปี งปม.', className: 'text-center word-wrap', type: 'html',
                                    field: '<span ng-if="row.SHARED_YR_TEMPLATE==1">ทุกปี งปม.</span>' +
                                        '<span class="text-danger" ng-if="row.SHARED_YR_TEMPLATE==2">บางปี งปม.</span>', style: 'width:100px;max-width:100px;min-width:100px;'
                                }
                            ]
                        }
                    };

                    // เมื่องบรายจ่าย เปลี่ยนแปลง
                    var budgetTypeChangedId = null;
                    $scope.budgetTypeChanged = function () { 
                        $timeout.cancel(budgetTypeChangedId);
                        budgetTypeChangedId = $timeout(function () {
                            $scope.$settings.expensesGroups = [];
                            $scope.$settings.formSearch.expensesGroupId = 'empty';
                            var budgetTypeId = ($scope.$settings.formSearch.budgetTypeId || '').replace(/[^\d]/ig, '');
                            if (budgetTypeId == '')
                                return;

                            $scope.$settings.isLoadExpensesGroup = true;
                            $customHttp.formPost($rootScope.baseUrl + '/Helper/RetrieveExpensesGroupByBudgetType', {
                                budgetTypeId: budgetTypeId
                            }).then(function (res) {
                                $scope.$settings.expensesGroups = res.data || [];
                                $scope.$settings.isLoadExpensesGroup = false;

                                // ส่งคำขอเพื่อค้นหา
                                $scope.submitSearch();
                            }, function () {
                               $scope.$settings.isLoadExpensesGroup = false;
                            });
                        }, 700);
                    };
                    // ส่งข้อมูลไปค้นหา
                    var submitSearchId = null;
                    $scope.submitSearch = function () {
                        $timeout.cancel(submitSearchId);
                        submitSearchId = $timeout(function () {
                            $scope.$broadcast('fwSimpleDataTable.budgetTemplate.modal.paramsChanged', {
                                templateName: $scope.$settings.formSearch.templateName,
                                budgetTypeFlag: $scope.$settings.formSearch.budgetTypeFlag, // 1 = เงิน งปม., 2 = เงินนอก งปม.
                                budgetTargetFlag: $scope.$settings.formSearch.budgetTargetFlag, // 1 = งบประมาณต้นปี, 2 = ของบประมาณเพิ่มเติม
                                planId: ($scope.$settings.formSearch.planId || '').replace('empty', ''),
                                produceId: ($scope.$settings.formSearch.produceId || '').replace('empty', ''),
                                activityId: ($scope.$settings.formSearch.activityId || '').replace('empty', ''),
                                budgetTypeId: ($scope.$settings.formSearch.budgetTypeId || '').replace('empty', ''),
                                expensesGroupId: ($scope.$settings.formSearch.expensesGroupId || '').replace('empty', ''),
                            })
                        }, 300);
                    };
                    // เมื่อ ชื่อเมนูเปลี่ยนแปลง
                    var templateNameChangedId = null;
                    $scope.templateNameChanged = function () {
                        $timeout.cancel(templateNameChangedId);
                        templateNameChangedId = $timeout(function () {
                            $scope.submitSearch();
                        }, 700);
                    };
                    // เมื่อมีการคลิกที่ แถวของข้อมูล
                    $scope.rowClick = function (event, row, columnConfig) {
                        if (columnConfig.params === 'BTN_SELECT' && event.target.tagName === 'A') {
                            $mdDialog.hide(row); // Return ค่าที่เลือก
                        }

                        return $q(function (resolve) {
                            resolve({ row: row });
                        });
                    }
                    // เมื่อต้องการสร้าง Template ใหม่
                    $scope.createNew = function (event) {
                        self.createOrUpdateBudgetRequestTemplate(event, null, '2'); // 2 = หน่วยงานเป็นผู้สร้าง Template ใช้เอง
                    };
                    // Close Modal
                    $scope.close = function () {
                        $mdDialog.cancel(null);
                    };

                    // กำหนดค่าพื้นฐาน
                    $scope.submitSearch();
                }, event).then(function (row) {
                    resolve(row);
                }, function () { });
            });
        };



        // ฟอร์มสำหรับสร้าง/แก้ไข รายการ Template คำของบประมาณ
        // row: (กรณีแก้ไข) ข้อมูล Template ที่ต้องการนำมาแก้ไข
        // createType: 1 = สร้างโดย Admin, 2 = สร้างโดยหน่วยงาน
        // สร้างหรือแก้ไข จะใช้ BudgetRequestTemplate เป็นตัวหลักในการทำงาน
        // Required Components: select2
        this.createOrUpdateBudgetRequestTemplate = function (event, row, createType) {
            return $q(function (resolve, reject) {
                // โหลด form template และแสดงผลบนหน้าเว็บไซด์
                var templateUrl = $rootScope.baseUrl + '/BudgetRequestTemplate/GetModalForm';
                $fwModalService.getModal(templateUrl, { $row: row || {}, $createType: createType || '1' }, function ($scope, $q, $rootScope, $fwDateService, $timeout, $row, $createType, $mdDialog, $fwDialogService, $fwModalHelperService, $customHttp) {
                    $scope.$settings = {
                        isSaving: false,
                        isLoadExpensesGroup: false, // โหลดข้อมูล หมวด คชจ.
                        isLoadExpenses: false, // โหลดข้อมูลรายการ คชจ.
                        checkAll: false, // เลือกรายการ คชจ. ทั้งหมด (Checkbox ส่วนหัวตารางของรายการ คชจ.)
                        expensesGroups: [], expenses: [],
                        formErrors: {},
                        formData: {}
                    };

                    // กำหนดค่าเริ่มต้นให้กับฟอร์ม
                    $scope.init = function (formData) {
                        $scope.$settings.formData = {
                            TemplateId: formData.TEMPLATE_ID || null,
                            TemplateName: formData.TEMPLATE_NAME || '',
                            CreateType: $createType,
                            PlanId: '' + (formData.PLAN_ID || 'empty'),
                            ProduceId: '' + (formData.PRODUCE_ID || 'empty'),
                            ActivityId: '' + (formData.ACTIVITY_ID || 'empty'),
                            BudgetTypeId: '' + (formData.BUDGET_TYPE_ID || 'empty'), // งบดำเนินงาน งบลงทุน งบอุดหนุน
                            ExpensesGroupId: '' + (formData.EXPENSES_GROUP_ID || 'empty'), // หมวด คชจ.
                            ForBudget: '1', // ใช้เฉพาะเงิน งปม.
                            ForOffBudget: '1', // ใช้เฉพาะเงินนอก งปม.
                            ForSourceBudgetBegin: '1', // เฉพาะคำของบประมาณต้นปี
                            ForSourceBudgetAdjunct: '1', // เฉพาะคำของบประมาณเพิ่มเติม
                            Expenses: [], // รายการค่าใช้จ่าย
                            SharedDepartment: 1, // ใช้ได้ทุกหน่วยงาน
                            SharedYear: 1, // ใช้ได้ทุกปี งปม.
                            Departments: [], // รหัสหน่วยงาน, ชื่อหน่วยงาน, จำกัดสิทธิ์การใช้ Template ตามหน่วยงาน (ไม่ระบุ = ได้ทุกหน่วยงาน)
                            Years: [] // จำกัดการเข้าถึง Template ตามปีงบประมาณ (ไม่ระบุ = ได้ทุกปี งปม.)
                        };


                        // กรณีเป็นการแก้ไขข้อมูลให้โหลด รายการ คชจ., สิทธิ์หน่วยงาน, สิทธิ์ปี งปม.
                        if ($scope.$settings.formData.TemplateId != null) {

                            // สิทธิ์การเข้าใช้งาน (หน่วยงาน, ปี งปม.)
                            if (formData.SHARED_DEP_TEMPLATE)
                                $scope.$settings.formData.SharedDepartment = formData.SHARED_DEP_TEMPLATE;
                            if (formData.SHARED_YR_TEMPLATE)
                                $scope.$settings.formData.SharedYear = formData.SHARED_YR_TEMPLATE;

                            // ใช้กับประเภทงบประมาณใด (งปม., เงินนอก งปม.)
                            if (formData.FOR_BUDGET)
                                $scope.$settings.formData.ForBudget = formData.FOR_BUDGET ? '1' : '2';
                            if (formData.FOR_OFF_BUDGET)
                                $scope.$settings.formData.ForOffBudget = formData.FOR_OFF_BUDGET ? '1' : '2';

                            // ใช้กับคำขอประเภทใด (คำขอต้นปี, คำขอ งปม. เพิ่มเติม)
                            if (formData.FOR_SOURCE_BUDGET_BEGIN)
                                $scope.$settings.formData.ForSourceBudgetBegin = formData.FOR_SOURCE_BUDGET_BEGIN ? '1' : '0';
                            if (formData.FOR_SOURCE_BUDGET_ADJUNCT)
                                $scope.$settings.formData.ForSourceBudgetAdjunct = formData.FOR_SOURCE_BUDGET_ADJUNCT ? '1' : '0';


                            // โหลดข้อมูลการมอบหมายสิทธิ์ (หน่วยงาน, ปี งปม.), รายการ คชจ. ที่กำหนดไว้
                            $scope.$settings.isLoading = true;
                            $customHttp.formPost($rootScope.baseUrl + '/Helper/RetrieveBudgetTemplateDetail', { templateId: $scope.$settings.formData.TemplateId }).then(function (res) {
                                $scope.$settings.formData.Expenses = res.data.Expenses || []; // id
                                $scope.$settings.formData.Departments = res.data.DepartmentAuthorize || []; // id, name
                                var years = res.data.YearAuthorize || []; // year
                                angular.forEach(years, function (year) {
                                    $scope.$settings.formData.Years.push({ year: $fwDateService.convertYearToBuddhist(year) });
                                });

                                // โหลดข้อมูล หมวด คชจ. และ รายการ คชจ. ภายใต้หมวด คชจ.
                                $scope.budgetTypeChanged().then(function () {
                                    $scope.$settings.formData.ExpensesGroupId = '' + formData.EXPENSES_GROUP_ID;
                                    $scope.expensesGroupChanged();
                                });

                                $scope.$settings.isLoading = false;
                            }, function () {
                                $scope.$settings.isLoading = false;
                            });
                        }


                        $timeout(function () {
                            $('#templateName').focus();
                        }, 300);
                    };

                    // งบรายการเปลี่ยนแปลง
                    $scope.budgetTypeChanged = function () {
                        return $q(function (resolve) {
                            $timeout(function () {
                                var budgetTypeId = $scope.$settings.formData.BudgetTypeId;
                                $scope.$settings.expensesGroups = [];
                                $scope.$settings.expenses = [];
                                $scope.$settings.formData.ExpensesGroupId = 'empty';
                                if ('empty' == budgetTypeId) {
                                    resolve({});
                                    return;
                                }

                                $scope.$settings.isLoadExpensesGroup = true;
                                $customHttp.formPost($rootScope.baseUrl + '/Helper/RetrieveExpensesGroupByBudgetType', {
                                    budgetTypeId: budgetTypeId
                                }).then(function (res) {
                                    $scope.$settings.expensesGroups = res.data || [];
                                    resolve($scope.$settings.expensesGroups);
                                    $scope.$settings.isLoadExpensesGroup = false;
                                }, function () {
                                    resolve({});
                                    $scope.$settings.isLoadExpensesGroup = false;
                                });
                            }, 200);
                        });
                    };
                    // เมื่อหมวด คชจ. เปลี่ยนแปลง
                    $scope.expensesGroupChanged = function () {
                        return $q(function (resolve) {
                            $timeout(function () {
                                var expensesGroupId = $scope.$settings.formData.ExpensesGroupId;
                                $scope.$settings.expenses = [];
                                if ('empty' == expensesGroupId) {
                                    resolve({});
                                    return;
                                }

                                $scope.$settings.isLoadExpenses = true;
                                $customHttp.formPost($rootScope.baseUrl + '/Helper/RetrieveExpensesByExpensesGroup', {
                                    expensesGroupId: expensesGroupId
                                }).then(function (res) {
                                    $scope.$settings.expenses = res.data || [];

                                    // Mark รายการที่ผู้ใช้เลือกไว้แล้ว
                                    $.each($scope.$settings.expenses, function (index, item) {
                                        // กรณีเป็นการสร้างรายการใหม่ให้ Default เลือกให้ทั้งหมด
                                        if (null == $scope.$settings.formData.TemplateId) {
                                            $scope.$settings.expenses[index] = $.extend(item, {
                                                checked: true
                                            });
                                        } else {
                                            $scope.$settings.expenses[index] = $.extend(item, {
                                                checked: $scope.$settings.formData.Expenses.indexOf(item.EXPENSES_ID) > -1
                                            });
                                        }
                                    });

                                    resolve($scope.$settings.expenses);
                                    $scope.$settings.isLoadExpenses = false;
                                }, function () {
                                    resolve({});
                                    $scope.$settings.isLoadExpenses = false;
                                });
                            }, 200);
                        });
                    };
                    // เมื่อกดเลือก ทั้งหมดของรายการ คชจ.
                    $scope.selectAll = function () {
                        $timeout(function () {
                            angular.forEach($scope.$settings.expenses, function (item) {
                                item.checked = $scope.$settings.checkAll;
                            });
                        }, 300);
                    };
                    // เลือกหน่วยงาน
                    $scope.selectDepartment = function (event) {
                        if ($createType != '1') // หน่วยงานสร้าง Template ไม่สามารถเลือกหน่วยงานได้
                            return;
                        $fwModalHelperService.getDepartmentSelectMultiModal(event, $scope.$settings.formData.Departments).then(function (selectedDepartments) {
                            $scope.$settings.formData.Departments = selectedDepartments || [];
                        });
                    };
                    // ส่งคำขอบันทึกข้อมูล
                    $scope.submitSave = function (event) {
                        $scope.$settings.isLoading = true;
                        $scope.$settings.formErrors = {};

                        var params = $.extend(true, {}, $scope.$settings.formData);
                        params.PlanId = (params.PlanId || '').replace(/[^\d]/g, '');
                        params.ProduceId = (params.ProduceId || '').replace(/[^\d]/g, '');
                        params.ActivityId = (params.ActivityId || '').replace(/[^\d]/g, '');
                        params.BudgetTypeId = (params.BudgetTypeId || '').replace(/[^\d]/g, '');
                        params.ExpensesGroupId = (params.ExpensesGroupId || '').replace(/[^\d]/g, '');

                        // ส่งค่าไปเฉพาะ ID
                        params.Departments = params.Departments.map(function (item) { return item.DEP_ID; });
                        // ส่งค่าไปเฉพาะ ID
                        params.Expenses = $scope.$settings.expenses.filter(function (item) { return item.checked; }).map(function (item) { return item.EXPENSES_ID; });
                        // แปลงปี พ.ศ. => ค.ศ.
                        params.Years = params.Years.map(function (item) { return $fwDateService.convertYearToBritish(item.year); });
                        $customHttp.formPost($rootScope.baseUrl + '/BudgetRequestTemplate/SubmitSave', params).then(function (res) {
                            $scope.$settings.formErrors = res.data.errors || {};
                            if (res.data.errorText != null)
                                $fwDialogService.dangerDialog(event, res.data.errorText);
                            else if (res.data.errors != null)
                                $fwDialogService.dangerDialog(event, 'โปรดตรวจสอบค่าต่างๆที่ระบบแจ้งให้เรียบร้อยก่อน');
                            else {
                                // ถ้าแก้ไขให้ปิดหน้าจอ
                                if ($scope.$settings.formData.TemplateId != null)
                                    $fwDialogService.alertDialog(event, 'แก้ไข Template เรียบร้อยแล้ว').then(function () {
                                        $scope.close();
                                    });
                                else {
                                    $fwDialogService.alertDialog(event, 'สร้าง Template เรียบร้อยแล้ว');
                                    $scope.init({});
                                }
                            }

                            $scope.$settings.isLoading = false;
                        }, function () {
                            $scope.$settings.isLoading = false;
                        });
                    };
                    // ปิดหน้าจอ
                    $scope.close = function () {
                        $mdDialog.hide();
                    };


                    // กำหนดค่าเริ่มต้นให้กับฟอร์ม
                    $scope.init($row || {});
                }, event).then(function () {
                    resolve();
                }, function () { reject(); });
            });
        };



        // ส่งคำขอดาวน์โหลดเอกสารจากระบบ
        // routeUrl: ส่งคำขอพิมพ์ไปที่ไหน
        // printRouteUrl: ส่งคำขอ Export file ไปที่ไหน (Resource/GetFile)
        this.printDocument = function (routeUrl, printRouteUrl, params) {
            return $q(function (resolve) {
                var href = $('<a href="javascript:void(0)" target="_blank">Export</a>');
                $('body').append(href);
                $customHttp.formPost(routeUrl, params || {}).then(function (res) {
                    href.prop('href', $filter('textFormat')('{0}&filename={1}&resultFilename={2}&deleteFlag=Y'
                        , printRouteUrl
                        , res.data.filename
                        , res.data.resultFilename || ''));
                    href[0].click();
                    resolve(res);
                }, function () {
                    resolve(null);
                });
            });
        }
    });
