import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5267/api';

export interface DocumentationDto {
  applicationName: string;
  primusClientId: string;
  stack: string;
  generatedAt: string;
  modules: DocumentationModuleDto[];
}

export interface DocumentationModuleDto {
  moduleName: string;
  version: string;
  isBreakingChange: boolean;
  releaseNotes: string;
  integrationSteps: string[];
  codeSnippets: Record<string, string>;
}

/**
 * Fetch generated documentation for a specific application
 * @param applicationId - The ID of the application
 * @returns Promise resolving to DocumentationDto
 */
export const fetchDocumentation = async (applicationId: string): Promise<DocumentationDto> => {
  const token = localStorage.getItem('token');
  
  const response = await axios.get<DocumentationDto>(
    `${API_BASE_URL}/documentation/${applicationId}`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  return response.data;
};
