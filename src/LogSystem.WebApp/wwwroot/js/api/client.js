/* ============================================================================
   API Client
   LogSystem WebApp - Centralized API communication
   ============================================================================ */

/**
 * Base API client class
 */
class ApiClient {
    constructor(baseUrl = '/api') {
        this.baseUrl = baseUrl;
    }

    /**
     * Make a fetch request with standardized error handling
     * @param {string} endpoint - API endpoint
     * @param {Object} options - Fetch options
     * @returns {Promise<any>} - Response data
     */
    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;

        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            }
        };

        const mergedOptions = { ...defaultOptions, ...options };

        try {
            const response = await fetch(url, mergedOptions);

            // Handle different response types
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ detail: response.statusText }));
                throw new ApiError(response.status, errorData.detail || response.statusText, errorData);
            }

            // Check if response has content
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                return await response.json();
            }

            return response;
        } catch (error) {
            if (error instanceof ApiError) {
                throw error;
            }
            throw new ApiError(0, 'Network error or server unavailable', { originalError: error.message });
        }
    }

    /**
     * GET request
     * @param {string} endpoint - API endpoint
     * @param {Object} options - Additional fetch options
     * @returns {Promise<any>} - Response data
     */
    async get(endpoint, options = {}) {
        return this.request(endpoint, { ...options, method: 'GET' });
    }

    /**
     * POST request
     * @param {string} endpoint - API endpoint
     * @param {Object} data - Request body data
     * @param {Object} options - Additional fetch options
     * @returns {Promise<any>} - Response data
     */
    async post(endpoint, data, options = {}) {
        return this.request(endpoint, {
            ...options,
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    /**
     * PUT request
     * @param {string} endpoint - API endpoint
     * @param {Object} data - Request body data
     * @param {Object} options - Additional fetch options
     * @returns {Promise<any>} - Response data
     */
    async put(endpoint, data, options = {}) {
        return this.request(endpoint, {
            ...options,
            method: 'PUT',
            body: JSON.stringify(data)
        });
    }

    /**
     * DELETE request
     * @param {string} endpoint - API endpoint
     * @param {Object} options - Additional fetch options
     * @returns {Promise<any>} - Response data
     */
    async delete(endpoint, options = {}) {
        return this.request(endpoint, { ...options, method: 'DELETE' });
    }
}

/**
 * Custom API Error class
 */
class ApiError extends Error {
    constructor(status, message, data = {}) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
        this.data = data;
    }
}

/**
 * LogCollections API
 */
class LogCollectionsApi extends ApiClient {
    constructor() {
        super('/api');
    }

    /**
     * Get all log collections
     * @returns {Promise<Array>} - Array of log collections
     */
    async getAll() {
        return this.get('/log-collections');
    }

    /**
     * Get a single log collection by ID
     * @param {number} id - Collection ID
     * @returns {Promise<Object>} - Log collection
     */
    async getById(id) {
        const collections = await this.getAll();
        const collection = collections.find(c => c.id === id);
        if (!collection) {
            throw new ApiError(404, 'Collection not found');
        }
        return collection;
    }

    /**
     * Create or update a log collection
     * @param {Object} data - Collection data
     * @returns {Promise<Object>} - Created/updated collection
     */
    async save(data) {
        return this.post('/log-collections', data);
    }

    /**
     * Delete a log collection
     * @param {number} id - Collection ID
     * @returns {Promise<void>}
     */
    async deleteById(id) {
        return this.delete(`/log-collections/${id}`);
    }

    /**
     * Get metrics for all collections
     * @returns {Promise<Array>} - Array of metrics
     */
    async getMetrics() {
        return this.get('/log-collections/metrics');
    }

    /**
     * Get log attributes for a collection
     * @param {number} collectionId - Collection ID
     * @returns {Promise<Array>} - Array of log attributes
     */
    async getAttributes(collectionId) {
        return this.get(`/log-collections/${collectionId}/log-attributes`);
    }

    /**
     * Search logs in a collection
     * @param {number} collectionId - Collection ID
     * @param {Object} searchParams - Search parameters (filters, lastId, limit)
     * @returns {Promise<Object>} - Search results
     */
    async searchLogs(collectionId, searchParams) {
        return this.post(`/log-collections/${collectionId}/logs/search`, searchParams);
    }
}

/**
 * LogAttributes API
 */
class LogAttributesApi extends ApiClient {
    constructor() {
        super('/api');
    }

    /**
     * Create a log attribute
     * @param {Object} data - Attribute data
     * @returns {Promise<Object>} - Created attribute
     */
    async create(data) {
        return this.post('/log-attributes', data);
    }

    /**
     * Update a log attribute
     * @param {Object} data - Attribute data with ID
     * @returns {Promise<Object>} - Updated attribute
     */
    async update(data) {
        return this.post('/log-attributes', data);
    }

    /**
     * Delete a log attribute
     * @param {number} id - Attribute ID
     * @returns {Promise<void>}
     */
    async deleteById(id) {
        return this.delete(`/log-attributes/${id}`);
    }
}

// Export singleton instances
export const logCollectionsApi = new LogCollectionsApi();
export const logAttributesApi = new LogAttributesApi();
export { ApiError };
