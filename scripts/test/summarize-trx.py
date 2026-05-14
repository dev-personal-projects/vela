#!/usr/bin/env python3

import argparse
import sys
import xml.etree.ElementTree as ET
from datetime import timedelta
from pathlib import Path


def strip_namespace(name):
    return name.rsplit("}", 1)[-1]


def children(element, local_name):
    return [child for child in element if strip_namespace(child.tag) == local_name]


def first_child(element, local_name):
    for child in element:
        if strip_namespace(child.tag) == local_name:
            return child
    return None


def parse_duration(value):
    if not value:
        return 0.0

    days = 0
    if "." in value and value.split(".", 1)[0].isdigit() and ":" in value:
        day_part, value = value.split(".", 1)
        days = int(day_part)

    pieces = value.split(":")
    if len(pieces) != 3:
        return 0.0

    hours = int(pieces[0])
    minutes = int(pieces[1])
    seconds = float(pieces[2])
    return (timedelta(days=days, hours=hours, minutes=minutes).total_seconds() + seconds)


def format_duration(seconds):
    if seconds >= 60:
        minutes = int(seconds // 60)
        remainder = seconds % 60
        return f"{minutes}m {remainder:04.1f}s"

    return f"{seconds:.2f}s"


def format_pass_rate(passed, total):
    return f"{((passed / total) * 100):.1f}%" if total else "0.0%"


def int_attr(element, name):
    if element is None:
        return 0

    raw = element.attrib.get(name, "0")
    try:
        return int(raw)
    except ValueError:
        return 0


def truncate(value, width):
    value = value.replace("\n", " ").strip()
    if len(value) <= width:
        return value

    if width <= 3:
        return value[:width]

    return value[: width - 3] + "..."


def print_table(title, headers, rows):
    print()
    print(title)
    print("-" * len(title))

    if not rows:
        print("No rows to display.")
        return

    widths = [len(header) for header in headers]
    for row in rows:
        for index, value in enumerate(row):
            widths[index] = max(widths[index], len(str(value)))

    separator = "+-" + "-+-".join("-" * width for width in widths) + "-+"
    header_row = "| " + " | ".join(headers[index].ljust(widths[index]) for index, width in enumerate(widths)) + " |"

    print(separator)
    print(header_row)
    print(separator)
    for row in rows:
        print("| " + " | ".join(str(row[index]).ljust(widths[index]) for index, width in enumerate(widths)) + " |")
    print(separator)


def suite_sort_key(suite):
    order = {
        "Unit": 0,
        "Integration": 1,
    }

    return (order.get(suite["suite"], 99), suite["suite"])


def suite_overview_row(suite):
    return [
        suite["suite"],
        suite["total"],
        suite["passed"],
        suite["failed"],
        suite["skipped"],
        format_pass_rate(suite["passed"], suite["total"]),
        format_duration(suite["duration"]),
        suite["result"],
    ]


def suite_detail_rows(suite):
    return [
        ["Total Tests", suite["total"]],
        ["Passed", suite["passed"]],
        ["Failed", suite["failed"]],
        ["Skipped", suite["skipped"]],
        ["Pass Rate", format_pass_rate(suite["passed"], suite["total"])],
        ["Duration", format_duration(suite["duration"])],
        ["Result", suite["result"]],
        ["TRX File", suite["path"].name],
    ]


def load_trx(path):
    root = ET.parse(path).getroot()
    result_summary = first_child(root, "ResultSummary")
    counters = first_child(result_summary, "Counters") if result_summary is not None else None
    results = first_child(root, "Results")
    suite_name = path.stem.replace("-tests", "").title()

    test_results = []
    if results is not None:
        for result in children(results, "UnitTestResult"):
            output = first_child(result, "Output")
            error_info = first_child(output, "ErrorInfo") if output is not None else None
            message = first_child(error_info, "Message") if error_info is not None else None
            test_results.append(
                {
                    "suite": suite_name,
                    "name": result.attrib.get("testName", "(unknown test)"),
                    "outcome": result.attrib.get("outcome", "Unknown"),
                    "duration": parse_duration(result.attrib.get("duration", "")),
                    "message": message.text.strip() if message is not None and message.text else "",
                }
            )

    failed = int_attr(counters, "failed") + int_attr(counters, "error") + int_attr(counters, "timeout") + int_attr(counters, "aborted")
    skipped = int_attr(counters, "notExecuted")
    total = int_attr(counters, "total")
    passed = int_attr(counters, "passed")
    duration = sum(result["duration"] for result in test_results)

    return {
        "suite": suite_name,
        "path": path,
        "total": total,
        "passed": passed,
        "failed": failed,
        "skipped": skipped,
        "duration": duration,
        "result": "Passed" if failed == 0 else "Failed",
        "tests": test_results,
    }


def main():
    parser = argparse.ArgumentParser(description="Render a readable summary for TRX test results.")
    parser.add_argument("results_dir", help="Directory containing one or more .trx files.")
    parser.add_argument("--top", type=int, default=10, help="Number of slowest tests to display.")
    args = parser.parse_args()

    results_dir = Path(args.results_dir)
    trx_files = sorted(results_dir.rglob("*.trx"))

    if not trx_files:
        print(f"No TRX files found under {results_dir}.", file=sys.stderr)
        return 1

    suites = sorted([load_trx(path) for path in trx_files], key=suite_sort_key)
    all_tests = [test for suite in suites for test in suite["tests"]]
    failed_tests = [test for test in all_tests if test["outcome"] not in {"Passed", "Completed"}]
    slowest_tests = sorted(all_tests, key=lambda test: test["duration"], reverse=True)[: args.top]

    suite_rows = [suite_overview_row(suite) for suite in suites]

    total = sum(suite["total"] for suite in suites)
    passed = sum(suite["passed"] for suite in suites)
    failed = sum(suite["failed"] for suite in suites)
    skipped = sum(suite["skipped"] for suite in suites)
    duration = sum(suite["duration"] for suite in suites)

    pass_rate = format_pass_rate(passed, total)
    suite_rows.append(["All", total, passed, failed, skipped, pass_rate, format_duration(duration), "Passed" if failed == 0 else "Failed"])

    print_table("Test Suite Summary", ["Suite", "Total", "Passed", "Failed", "Skipped", "Pass Rate", "Duration", "Result"], suite_rows)

    for suite in suites:
        print_table(f"{suite['suite']} Test Summary", ["Metric", "Value"], suite_detail_rows(suite))

    slow_rows = [
        [
            test["suite"],
            format_duration(test["duration"]),
            truncate(test["name"], 80),
            test["outcome"],
        ]
        for test in slowest_tests
    ]
    print_table("Slowest Tests", ["Suite", "Duration", "Test", "Outcome"], slow_rows)

    failure_rows = [
        [
            test["suite"],
            truncate(test["name"], 70),
            truncate(test["message"] or test["outcome"], 90),
        ]
        for test in failed_tests
    ]
    print_table("Failures", ["Suite", "Test", "Message"], failure_rows)

    print()
    print(f"TRX results: {results_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
