﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace MOE.Common.Business.Bins
{

    public static class BinFactory
    {
        public static List<BinsContainer> GetBins(BinFactoryOptions timeOptions)
        {
            
            switch (timeOptions.BinSize)
            {
                case BinFactoryOptions.BinSizes.FifteenMinutes:
                    return GetBinsForRange(timeOptions, 15);
                case BinFactoryOptions.BinSizes.ThirtyMinutes:
                    return GetBinsForRange(timeOptions, 30);
                case BinFactoryOptions.BinSizes.Hour:
                    return GetBinsForRange(timeOptions, 60);
                case BinFactoryOptions.BinSizes.Day:
                    return GetDayBinsContainersForRange(timeOptions);
                case BinFactoryOptions.BinSizes.Week:
                    return GetBinsForRange(timeOptions, 60*24*7);
                case BinFactoryOptions.BinSizes.Month:
                    return GetMonthBinsForRange(timeOptions);
                case BinFactoryOptions.BinSizes.Year:
                    return GetYearBinsForRange(timeOptions);
                default:
                    return GetBinsForRange(timeOptions, 15);
            }
        }

        private static List<Bin> GetDayBinsForRange(DateTime startDate, DateTime endDate, int startHour, int startMinute, int endHour, int endMinute, List<DayOfWeek> daysOfWeek)
        {
            List<Bin> bins = new List<Bin>();
            for (DateTime startTime = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0); startTime.Date < endDate.Date; startTime = startTime.AddDays(1))
            {
                if (daysOfWeek.Contains(startTime.DayOfWeek))
                {
                    bins.Add(new Bin
                    {
                        Start = startTime.AddHours(startHour).AddMinutes(startMinute),
                        End = startTime.AddHours(endHour).AddMinutes(endMinute)
                    });
                }
            }
            return bins;
        }

        private static List<BinsContainer> GetDayBinsContainersForRange(BinFactoryOptions timeOptions)
        {
            List<BinsContainer> binsContainers = new List<BinsContainer>();
            BinsContainer binsContainer = new BinsContainer();
            for (DateTime startTime = new DateTime(timeOptions.Start.Year, timeOptions.Start.Month, timeOptions.Start.Day,0,0,0); startTime.Date <= timeOptions.End.Date; startTime = startTime.AddDays(1))
            {
                if (timeOptions.TimeOption == BinFactoryOptions.TimeOptions.StartToEnd)
                {
                    binsContainer.Bins.Add(new Bin { Start = startTime, End = startTime.AddDays(1) });
                }
                else
                {
                    if (timeOptions.TimeOfDayStartHour != null && timeOptions.TimeOfDayStartMinute != null && timeOptions.TimeOfDayEndHour != null && timeOptions.TimeOfDayEndMinute != null)
                            binsContainer.Bins.Add(new Bin
                            { 
                                Start = startTime.AddHours(timeOptions.TimeOfDayStartHour.Value)
                                    .AddMinutes(timeOptions.TimeOfDayStartMinute.Value),
                                End = startTime.AddHours(timeOptions.TimeOfDayEndHour.Value)
                                    .AddMinutes(timeOptions.TimeOfDayEndMinute.Value)
                            });
                }
            }
            binsContainers.Add(binsContainer);
            return binsContainers;
        }

        private static List<BinsContainer> GetYearBinsForRange(BinFactoryOptions timeOptions)
        {
            List<BinsContainer> binsContainers = new List<BinsContainer>();
            for (DateTime startTime = new DateTime(timeOptions.Start.Year, timeOptions.Start.Month, 1); startTime.Year <= timeOptions.End.Year && startTime.Month <= timeOptions.End.Month; startTime = startTime.AddYears(1))
            {
                BinsContainer binsContainer = new BinsContainer();
                if (timeOptions.TimeOption == BinFactoryOptions.TimeOptions.StartToEnd)
                {
                    binsContainer.Bins.Add(new Bin { Start = startTime, End = startTime.AddYears(1) });
                }
                else
                {
                    for (DateTime tempDate = startTime; tempDate < startTime.AddYears(1); tempDate = tempDate.AddDays(1))
                    {
                        if (timeOptions.DaysOfWeek.Contains(tempDate.DayOfWeek))
                        {
                            DateTime startDate = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day,
                                timeOptions.TimeOfDayStartHour ?? 0,
                                timeOptions.TimeOfDayStartMinute ?? 0, 0);
                            DateTime endDate = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day,
                                timeOptions.TimeOfDayEndHour ?? 11,
                                timeOptions.TimeOfDayEndMinute ?? 59, 0);
                            Bin bin = new Bin {Start = startDate, End = endDate};
                            binsContainer.Bins.Add(bin);
                        }
                    }
                }
                binsContainers.Add(binsContainer);
            }
            return binsContainers;
        }

        private static List<BinsContainer> GetMonthBinsForRange(BinFactoryOptions timeOptions)
        {
            List<BinsContainer> binsContainers = new List<BinsContainer>();
            if (timeOptions.TimeOption == BinFactoryOptions.TimeOptions.StartToEnd)
            {
                BinsContainer binsContainer = new BinsContainer();
                for (DateTime startTime = new DateTime(timeOptions.Start.Year, timeOptions.Start.Month, 1);
                    startTime.Date <= timeOptions.End.Date;
                    startTime = startTime.AddMonths(1))
                {
                    binsContainer.Bins.Add(new Bin {Start = startTime, End = startTime.AddMonths(1)});
                }
                binsContainers.Add(binsContainer);
            }
            else
            {
                for (DateTime startTime = new DateTime(timeOptions.Start.Year, timeOptions.Start.Month, 1);
                    startTime.Date <= timeOptions.End.Date;
                    startTime = startTime.AddMonths(1))
                {
                    binsContainers.Add(new BinsContainer
                    {
                        Start = startTime,
                        End = startTime.AddMonths(1),
                        Bins = GetDayBinsForRange(startTime, startTime.AddMonths(1), timeOptions.TimeOfDayStartHour.Value, timeOptions.TimeOfDayStartMinute.Value, timeOptions.TimeOfDayEndHour.Value, timeOptions.TimeOfDayEndMinute.Value, timeOptions.DaysOfWeek)
                    });
                }
            }
            return binsContainers;
        }

        private static List<BinsContainer> GetBinsForRange(BinFactoryOptions timeOptions, int minutes)
        {
            List<BinsContainer> binsContainers = new List<BinsContainer>();
            TimeSpan startTimeSpan = new TimeSpan();
            TimeSpan endTimeSpan = new TimeSpan();
            if (timeOptions.TimeOption == BinFactoryOptions.TimeOptions.TimePeriod &&
                timeOptions.TimeOfDayStartHour != null &&
                timeOptions.TimeOfDayStartMinute != null &&
                timeOptions.TimeOfDayEndHour != null &&
                timeOptions.TimeOfDayEndMinute != null)
            {
                startTimeSpan = new TimeSpan(0, timeOptions.TimeOfDayStartHour.Value,
                    timeOptions.TimeOfDayStartMinute.Value, 0);
                endTimeSpan = new TimeSpan(0, timeOptions.TimeOfDayEndHour.Value,
                    timeOptions.TimeOfDayEndMinute.Value, 0);
            }
            BinsContainer binsContainer = new BinsContainer();
            for (DateTime startTime = timeOptions.Start; startTime < timeOptions.End; startTime = startTime.AddMinutes(minutes))
            {
                if (timeOptions.TimeOption == BinFactoryOptions.TimeOptions.StartToEnd)
                {
                    binsContainer.Bins.Add(new Bin { Start = startTime, End = startTime.AddMinutes(minutes) });
                }
                else
                {
                    TimeSpan periodStartTimeSpan = new TimeSpan(0, startTime.Hour,
                        startTime.Minute, 0);
                    if (timeOptions.DaysOfWeek.Contains(startTime.DayOfWeek) &&
                        periodStartTimeSpan >= startTimeSpan &&
                        periodStartTimeSpan < endTimeSpan)
                    {
                        binsContainer.Bins.Add(new Bin { Start = startTime, End = startTime.AddMinutes(minutes) });
                    }
                }
            }
            binsContainers.Add(binsContainer);
            return binsContainers;
        }
    }
}