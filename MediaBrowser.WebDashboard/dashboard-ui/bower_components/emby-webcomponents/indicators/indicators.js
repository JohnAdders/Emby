define(["datetime","itemHelper","css!./indicators.css","material-icons"],function(datetime,itemHelper){"use strict";function enableProgressIndicator(item){return"Video"===item.MediaType&&"TvChannel"!==item.Type||("AudioBook"===item.Type||"AudioPodcast"===item.Type)}function getProgressHtml(pct,options){var containerClass="itemProgressBar";return options&&options.containerClass&&(containerClass+=" "+options.containerClass),'<div class="'+containerClass+'"><div class="itemProgressBarForeground" style="width:'+pct+'%;"></div></div>'}function getAutoTimeProgressHtml(pct,options,isRecording,start,end){var containerClass="itemProgressBar";options&&options.containerClass&&(containerClass+=" "+options.containerClass);var foregroundClass="itemProgressBarForeground";return isRecording&&(foregroundClass+=" itemProgressBarForeground-recording"),'<div is="emby-progressbar" data-automode="time" data-starttime="'+start+'" data-endtime="'+end+'" class="'+containerClass+'"><div class="'+foregroundClass+'" style="width:'+pct+'%;"></div></div>'}function getProgressBarHtml(item,options){var pct;if(enableProgressIndicator(item)&&"Recording"!==item.Type){var userData=options?options.userData||item.UserData:item.UserData;if(userData&&(pct=userData.PlayedPercentage,pct&&pct<100))return getProgressHtml(pct,options)}if(("Program"===item.Type||"Timer"===item.Type||"Recording"===item.Type)&&item.StartDate&&item.EndDate){var startDate=0,endDate=1;try{startDate=datetime.parseISO8601Date(item.StartDate).getTime()}catch(err){}try{endDate=datetime.parseISO8601Date(item.EndDate).getTime()}catch(err){}var now=(new Date).getTime(),total=endDate-startDate;if(pct=100*((now-startDate)/total),pct>0&&pct<100){var isRecording="Timer"===item.Type||"Recording"===item.Type||item.TimerId;return getAutoTimeProgressHtml(pct,options,isRecording,startDate,endDate)}}return""}function enablePlayedIndicator(item){return itemHelper.canMarkPlayed(item)}function getPlayedIndicator(item){if(enablePlayedIndicator(item)){var userData=item.UserData||{};if(userData.UnplayedItemCount)return'<div class="countIndicator indicator">'+userData.UnplayedItemCount+"</div>";if(userData.PlayedPercentage&&userData.PlayedPercentage>=100||userData.Played)return'<div class="playedIndicator indicator"><i class="md-icon indicatorIcon">&#xE5CA;</i></div>'}return""}function getCountIndicatorHtml(count){return'<div class="countIndicator indicator">'+count+"</div>"}function getChildCountIndicatorHtml(item,options){var minCount=0;return options&&(minCount=options.minCount||minCount),item.ChildCount&&item.ChildCount>minCount?getCountIndicatorHtml(item.ChildCount):""}function getTimerIndicator(item){var status;if("SeriesTimer"===item.Type)return'<i class="md-icon timerIndicator indicatorIcon">&#xE062;</i>';if(item.TimerId||item.SeriesTimerId)status=item.Status||"Cancelled";else{if("Timer"!==item.Type)return"";status=item.Status}return item.SeriesTimerId?"Cancelled"!==status?'<i class="md-icon timerIndicator indicatorIcon">&#xE062;</i>':'<i class="md-icon timerIndicator timerIndicator-inactive indicatorIcon">&#xE062;</i>':'<i class="md-icon timerIndicator indicatorIcon">&#xE061;</i>'}function getSyncIndicator(item){return 100===item.SyncPercent?'<div class="syncIndicator indicator fullSyncIndicator"><i class="md-icon indicatorIcon">&#xE2C4;</i></div>':null!=item.SyncPercent?'<div class="syncIndicator indicator emptySyncIndicator"><i class="md-icon indicatorIcon">&#xE2C4;</i></div>':""}function getVideoIndicator(item){return"Video"===item.MediaType?'<div class="indicator videoIndicator"><i class="md-icon indicatorIcon">&#xE04B;</i></div>':""}function onAutoTimeProgress(){var start=parseInt(this.getAttribute("data-starttime")),end=parseInt(this.getAttribute("data-endtime")),now=(new Date).getTime(),total=end-start,pct=100*((now-start)/total);pct=Math.min(100,pct),pct=Math.max(0,pct);var itemProgressBarForeground=this.querySelector(".itemProgressBarForeground");itemProgressBarForeground.style.width=pct+"%"}var ProgressBarPrototype=Object.create(HTMLDivElement.prototype);return ProgressBarPrototype.attachedCallback=function(){this.timeInterval&&clearInterval(this.timeInterval),"time"===this.getAttribute("data-automode")&&(this.timeInterval=setInterval(onAutoTimeProgress.bind(this),6e4))},ProgressBarPrototype.detachedCallback=function(){this.timeInterval&&(clearInterval(this.timeInterval),this.timeInterval=null)},document.registerElement("emby-progressbar",{prototype:ProgressBarPrototype,extends:"div"}),{getProgressBarHtml:getProgressBarHtml,getPlayedIndicatorHtml:getPlayedIndicator,getChildCountIndicatorHtml:getChildCountIndicatorHtml,enableProgressIndicator:enableProgressIndicator,getTimerIndicator:getTimerIndicator,enablePlayedIndicator:enablePlayedIndicator,getSyncIndicator:getSyncIndicator,getVideoIndicator:getVideoIndicator}});