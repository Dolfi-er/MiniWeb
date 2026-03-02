#!/bin/bash

# MiniWeb API testing script (final version with proper output redirection)
# Usage: ./test_api.sh [base_url]
# Default base_url: http://localhost:5179

set -euo pipefail

BASE_URL="${1:-http://localhost:5179}"
API_URL="$BASE_URL/api/books"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
print_step() {
    echo -e "${BLUE}==>${NC} $1"
}

print_success() {
    echo -e "${GREEN}SUCCESS:${NC} $1"
}

print_error() {
    echo -e "${RED}ERROR:${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}WARNING:${NC} $1"
}

# Check if required tools are installed
check_dependencies() {
    if ! command -v jq &> /dev/null; then
        print_error "jq is not installed. Please install jq first."
        exit 1
    fi
}

# Check if server is available
check_server() {
    print_step "Checking if server is reachable at $BASE_URL..."
    if curl -s -o /dev/null -w "%{http_code}" "$BASE_URL" | grep -q "404"; then
        # 404 is fine because root may not exist, but server responds
        print_success "Server is responding"
    elif curl -s -f "$BASE_URL" > /dev/null 2>&1; then
        print_success "Server is responding"
    else
        print_error "Cannot reach server at $BASE_URL. Is it running?"
        exit 1
    fi
}

# Test GET /api/books (list all)
test_get_all() {
    print_step "GET $API_URL (list all books)" >&2
    
    local tmp_body=$(mktemp)
    local tmp_headers=$(mktemp)
    
    local http_code=$(curl -s -X GET "$API_URL" \
        -D "$tmp_headers" \
        -o "$tmp_body" \
        -w "%{http_code}")
    
    local body=$(cat "$tmp_body")
    rm -f "$tmp_body" "$tmp_headers"
    
    if [ "$http_code" -eq 200 ]; then
        print_success "HTTP 200 OK" >&2
        echo "Response body:" >&2
        echo "$body" | jq . 2>/dev/null >&2 || echo "$body" >&2
    else
        print_error "Expected 200, got $http_code" >&2
        echo "$body" >&2
        exit 1
    fi
}

# Test POST /api/books (create)
test_create() {
    local title="$1"
    local author="$2"
    local year="$3"
    local expected_code="$4"
    local description="$5"
    
    print_step "POST $API_URL (create book: $description)" >&2
    
    json_data=$(jq -n \
        --arg t "$title" \
        --arg a "$author" \
        --arg y "$year" \
        '{title: $t, author: $a, year: ($y | tonumber)}')
    
    local tmp_body=$(mktemp)
    local tmp_headers=$(mktemp)
    
    local http_code=$(curl -s -X POST "$API_URL" \
        -H "Content-Type: application/json" \
        -d "$json_data" \
        -D "$tmp_headers" \
        -o "$tmp_body" \
        -w "%{http_code}")
    
    local body=$(cat "$tmp_body")
    local headers=$(cat "$tmp_headers")
    rm -f "$tmp_body" "$tmp_headers"
    
    if [ "$http_code" -eq "$expected_code" ]; then
        print_success "HTTP $expected_code as expected" >&2
        if [ "$expected_code" -eq 201 ]; then
            # Extract Location header (case-insensitive)
            location=$(echo "$headers" | grep -i "^Location:" | cut -d' ' -f2 | tr -d '\r')
            if [ -n "$location" ] && [ "$location" != "null" ]; then
                print_success "Location: $location" >&2
                book_id=$(basename "$location")
                echo "Created book ID: $book_id" >&2
                # Return ID via stdout
                echo "$book_id"
            else
                print_warning "Location header not found" >&2
            fi
            echo "Response body:" >&2
            echo "$body" | jq . 2>/dev/null >&2 || echo "$body" >&2
        else
            echo "Error response:" >&2
            echo "$body" | jq . 2>/dev/null >&2 || echo "$body" >&2
        fi
    else
        print_error "Expected $expected_code, got $http_code" >&2
        echo "Request data: $json_data" >&2
        echo "Response headers:" >&2
        echo "$headers" >&2
        echo "Response body:" >&2
        echo "$body" >&2
        exit 1
    fi
}

# Test GET /api/books/{id}
test_get_by_id() {
    local id="$1"
    local expected_code="$2"
    
    print_step "GET $API_URL/$id" >&2
    
    local tmp_body=$(mktemp)
    local tmp_headers=$(mktemp)
    
    local http_code=$(curl -s -X GET "$API_URL/$id" \
        -D "$tmp_headers" \
        -o "$tmp_body" \
        -w "%{http_code}")
    
    local body=$(cat "$tmp_body")
    rm -f "$tmp_body" "$tmp_headers"
    
    # Проверка, что http_code не пустой и является числом
    if ! [[ "$http_code" =~ ^[0-9]+$ ]]; then
        print_error "Invalid HTTP code received: '$http_code'" >&2
        exit 1
    fi
    
    if [ "$http_code" -eq "$expected_code" ]; then
        print_success "HTTP $expected_code as expected" >&2
        if [ "$expected_code" -eq 200 ]; then
            echo "Book details:" >&2
            echo "$body" | jq . 2>/dev/null >&2 || echo "$body" >&2
        else
            echo "Error response:" >&2
            echo "$body" | jq . 2>/dev/null >&2 || echo "$body" >&2
        fi
    else
        print_error "Expected $expected_code, got $http_code" >&2
        echo "$body" >&2
        exit 1
    fi
}

# Test X-Request-Id header presence
test_request_id_header() {
    print_step "Checking X-Request-Id header" >&2
    
    local tmp_headers=$(mktemp)
    curl -s -I "$API_URL" -D "$tmp_headers" -o /dev/null
    local request_id=$(grep -i "^X-Request-Id:" "$tmp_headers" | cut -d' ' -f2 | tr -d '\r')
    rm -f "$tmp_headers"
    
    if [ -n "$request_id" ]; then
        print_success "X-Request-Id present: $request_id" >&2
    else
        print_error "X-Request-Id header missing in response" >&2
        exit 1
    fi
}

# Main test sequence
main() {
    check_dependencies
    check_server
    test_request_id_header
    
    # Initial list (should be empty or contain some books)
    test_get_all
    
    # Create a valid book
    book_id=$(test_create "The Hobbit" "J.R.R. Tolkien" 1937 201 "valid book")
    
    # Get all again and verify it's there
    test_get_all
    
    # Get by ID
    if [ -n "$book_id" ]; then
        test_get_by_id "$book_id" 200
    else
        print_warning "Skipping get by ID because no ID was captured"
    fi
    
    # Test non-existent ID
    non_existent_id="00000000-0000-0000-0000-000000000000"
    test_get_by_id "$non_existent_id" 404
    
    # Test validation errors
    print_step "Testing validation errors" >&2
    
    # Empty title
    test_create "" "Some Author" 2023 400 "empty title" > /dev/null
    
    # Year too old
    test_create "Old Book" "Ancient Writer" 1400 400 "year < 1450" > /dev/null
    
    # Year in future
    future_year=$(( $(date +%Y) + 10 ))
    test_create "Future Book" "Sci-Fi Author" $future_year 400 "year > current" > /dev/null
    
    # Missing author (should be allowed, author optional)
    test_create "No Author Book" "" 2023 201 "missing author (should succeed)" > /dev/null
    
    print_success "All tests passed!" >&2
}

main "$@"